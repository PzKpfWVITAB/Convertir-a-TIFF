Imports System.Collections.Concurrent
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports MySqlConnector

''' <summary>
''' Índice permanente de PDFs de origen (tabla indice_pdf_origen en la misma base de datos).
''' Guarda ruta completa, nombre, número de páginas, tamaño y fecha de modificación de cada PDF,
''' de modo que el pre-chequeo y el reparador conozcan las páginas esperadas sin abrir el PDF por red.
''' </summary>
Public Module ModuloIndiceOrigen

    Public Const NombreTablaIndice As String = "indice_pdf_origen"
    Private Const TamanoLote As Integer = 500

    ''' <summary>Datos indexados de un PDF de origen.</summary>
    Public Class InfoIndicePdf
        Public Property Paginas As Integer = 0
        Public Property Tamano As Long = 0
        Public Property FechaMod As DateTime? = Nothing
        Public Property Estatus As String = "OK"
    End Class

    ''' <summary>Resumen de lo indexado bajo un directorio de origen.</summary>
    Public Class ResumenIndice
        Public Property TotalPdfs As Integer = 0
        Public Property TotalPaginas As Long = 0
        Public Property TotalSinLeer As Integer = 0
        Public Property FechaIndexado As DateTime? = Nothing
        Public Property Mensaje As String = ""

        Public ReadOnly Property Existe As Boolean
            Get
                Return TotalPdfs > 0
            End Get
        End Property

        Public Overrides Function ToString() As String
            If Not String.IsNullOrEmpty(Mensaje) Then Return Mensaje
            If Not Existe Then Return "Índice: esta ruta aún no está indexada."
            Dim txt = $"Índice: {TotalPdfs:#,##0} PDFs · {TotalPaginas:#,##0} páginas"
            If TotalSinLeer > 0 Then txt &= $" · {TotalSinLeer:#,##0} sin leer"
            If FechaIndexado.HasValue Then txt &= $" · indexado {FechaIndexado.Value:yyyy-MM-dd HH:mm}"
            Return txt
        End Function
    End Class

    Private Class FilaIndice
        Public Ruta As String = ""
        Public Nombre As String = ""
        Public Paginas As Integer = 0
        Public Tamano As Long = 0
        Public FechaMod As DateTime
        Public Estatus As String = "OK"
    End Class

    Private Function NormalizarOrigen(ruta As String) As String
        If String.IsNullOrWhiteSpace(ruta) Then Return ""
        Return ruta.Trim().TrimEnd("\"c, "/"c)
    End Function

    ''' <summary>
    ''' Patrón LIKE (con ESCAPE '|') que abarca todo lo que cuelga del directorio indicado.
    ''' </summary>
    Private Function PatronPrefijo(origen As String) As String
        Dim sb As New StringBuilder()
        For Each c As Char In NormalizarOrigen(origen) & "\"
            If c = "|"c OrElse c = "%"c OrElse c = "_"c Then sb.Append("|"c)
            sb.Append(c)
        Next
        sb.Append("%"c)
        Return sb.ToString()
    End Function

    ''' <summary>Crea la tabla del índice si no existe.</summary>
    Public Sub AsegurarTablaIndice()
        Using conn As New MySqlConnection(ModuloConversionJpg.CadenaConexionMysql)
            conn.Open()
            Dim sql = $"CREATE TABLE IF NOT EXISTS {NombreTablaIndice} (" &
                      "id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY, " &
                      "ruta_completa VARCHAR(1000) NOT NULL, " &
                      "nombre VARCHAR(255) NOT NULL, " &
                      "paginas INT NOT NULL DEFAULT 0, " &
                      "tamano BIGINT NOT NULL DEFAULT 0, " &
                      "fecha_mod DATETIME NULL, " &
                      "estatus VARCHAR(20) NOT NULL DEFAULT 'OK', " &
                      "fecha_indexado DATETIME NOT NULL, " &
                      "INDEX idx_ruta (ruta_completa(190))" &
                      ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"
            Using cmd As New MySqlCommand(sql, conn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Cuenta cuántos PDFs y páginas hay indexados bajo el directorio de origen.
    ''' </summary>
    Public Function ObtenerResumenIndice(origen As String) As ResumenIndice
        Dim r As New ResumenIndice()
        If String.IsNullOrWhiteSpace(origen) Then Return r
        Try
            Using conn As New MySqlConnection(ModuloConversionJpg.CadenaConexionMysql)
                conn.Open()
                Dim sql = $"SELECT COUNT(*), COALESCE(SUM(paginas),0), COALESCE(SUM(estatus<>'OK'),0), MAX(fecha_indexado) " &
                          $"FROM {NombreTablaIndice} WHERE ruta_completa LIKE @p ESCAPE '|'"
                Using cmd As New MySqlCommand(sql, conn)
                    cmd.CommandTimeout = 120
                    cmd.Parameters.AddWithValue("@p", PatronPrefijo(origen))
                    Using dr = cmd.ExecuteReader()
                        If dr.Read() Then
                            r.TotalPdfs = Convert.ToInt32(dr(0))
                            r.TotalPaginas = Convert.ToInt64(dr(1))
                            r.TotalSinLeer = Convert.ToInt32(dr(2))
                            If Not IsDBNull(dr(3)) Then r.FechaIndexado = Convert.ToDateTime(dr(3))
                        End If
                    End Using
                End Using
            End Using
        Catch exTbl As MySqlException When exTbl.Number = 1146
            ' La tabla aún no existe: simplemente no hay índice.
        Catch ex As Exception
            r = New ResumenIndice With {.Mensaje = "Índice: no se pudo consultar la base de datos (" & ex.Message & ")"}
        End Try
        Return r
    End Function

    Private Sub BorrarIndiceDirectorio(origen As String)
        Using conn As New MySqlConnection(ModuloConversionJpg.CadenaConexionMysql)
            conn.Open()
            Using cmd As New MySqlCommand($"DELETE FROM {NombreTablaIndice} WHERE ruta_completa LIKE @p ESCAPE '|'", conn)
                cmd.CommandTimeout = 600
                cmd.Parameters.AddWithValue("@p", PatronPrefijo(origen))
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Enumera archivos PDF en paralelo sobre almacenamiento local o red SMB aprovechando todos los núcleos de CPU.
    ''' Divide los subdirectorios de primer y segundo nivel en tareas paralelas para saturar el enlace de red.
    ''' </summary>
    Public Function EnumerarArchivosPdfParalelo(origen As String, token As CancellationToken, hilos As Integer) As FileInfo()
        Dim listaArchivos As New ConcurrentBag(Of FileInfo)()
        Dim diOrigen As New DirectoryInfo(origen)
        If Not diOrigen.Exists Then Return Array.Empty(Of FileInfo)()

        ' 1. Archivos en la raíz del origen
        Try
            For Each fi In diOrigen.EnumerateFiles("*.pdf", SearchOption.TopDirectoryOnly)
                listaArchivos.Add(fi)
            Next
        Catch
        End Try

        ' 2. Obtener subdirectorios de primer nivel
        Dim subDirs As DirectoryInfo() = Nothing
        Try
            subDirs = diOrigen.GetDirectories()
        Catch
            subDirs = Array.Empty(Of DirectoryInfo)()
        End Try

        If subDirs.Length = 0 Then
            Return listaArchivos.ToArray()
        End If

        ' 2. Expandir carpetas por amplitud (BFS) hasta tener suficientes ramas para saturar todos los núcleos
        Dim colaCarpetas As New Queue(Of DirectoryInfo)(subDirs)
        Dim carpetasATrabajar As New List(Of DirectoryInfo)()
        Dim metaRamas = Math.Max(hilos * 2, 64)

        While colaCarpetas.Count > 0 AndAlso (carpetasATrabajar.Count + colaCarpetas.Count) < metaRamas
            Dim dirActual = colaCarpetas.Dequeue()
            Try
                Dim subNivel = dirActual.GetDirectories()
                If subNivel.Length > 0 Then
                    For Each s In subNivel
                        colaCarpetas.Enqueue(s)
                    Next
                    ' Recoger archivos de este nivel intermedio
                    For Each fi In dirActual.EnumerateFiles("*.pdf", SearchOption.TopDirectoryOnly)
                        listaArchivos.Add(fi)
                    Next
                Else
                    carpetasATrabajar.Add(dirActual)
                End If
            Catch
                carpetasATrabajar.Add(dirActual)
            End Try
        End While

        carpetasATrabajar.AddRange(colaCarpetas)

        ' 3. Exploración concurrente multinúcleo de cada subárbol
        Dim popt As New ParallelOptions With {
            .MaxDegreeOfParallelism = Math.Max(2, hilos),
            .CancellationToken = token
        }

        Parallel.ForEach(carpetasATrabajar, popt, Sub(dirInfo)
            If token.IsCancellationRequested Then Exit Sub
            Try
                For Each fi In dirInfo.EnumerateFiles("*.pdf", SearchOption.AllDirectories)
                    If token.IsCancellationRequested Then Exit For
                    listaArchivos.Add(fi)
                Next
            Catch
            End Try
        End Sub)

        Return listaArchivos.ToArray()
    End Function

    Private Sub InsertarLoteConConexion(conn As MySqlConnection, lote As List(Of FilaIndice))
        If lote Is Nothing OrElse lote.Count = 0 Then Exit Sub
        Using trans = conn.BeginTransaction()
            Using cmd As New MySqlCommand()
                cmd.Connection = conn
                cmd.Transaction = trans
                cmd.CommandTimeout = 120
                Dim sb As New StringBuilder($"INSERT INTO {NombreTablaIndice} (ruta_completa, nombre, paginas, tamano, fecha_mod, estatus, fecha_indexado) VALUES ")
                For i As Integer = 0 To lote.Count - 1
                    If i > 0 Then sb.Append(","c)
                    sb.Append($"(@r{i},@n{i},@p{i},@t{i},@m{i},@e{i},NOW())")
                    cmd.Parameters.AddWithValue($"@r{i}", lote(i).Ruta)
                    cmd.Parameters.AddWithValue($"@n{i}", lote(i).Nombre)
                    cmd.Parameters.AddWithValue($"@p{i}", lote(i).Paginas)
                    cmd.Parameters.AddWithValue($"@t{i}", lote(i).Tamano)
                    cmd.Parameters.AddWithValue($"@m{i}", lote(i).FechaMod)
                    cmd.Parameters.AddWithValue($"@e{i}", lote(i).Estatus)
                Next
                cmd.CommandText = sb.ToString()
                cmd.ExecuteNonQuery()
            End Using
            trans.Commit()
        End Using
    End Sub

    ''' <summary>
    ''' Reconstruye desde cero el índice del directorio indicado a máxima potencia de hardware (90% CPU/RAM/Red).
    ''' 1. Enumera archivos en paralelo dividiendo subcarpetas sobre SMB.
    ''' 2. Lee páginas con motor ultrarrápido (1ms por inspección binaria).
    ''' 3. Inserción masiva asíncrona en MySQL sin bloquear hilos de lectura.
    ''' onProgreso recibe (procesados, total, fase).
    ''' </summary>
    Public Function ReindexarDirectorio(
        origen As String,
        hilos As Integer,
        token As CancellationToken,
        Optional onProgreso As Action(Of Integer, Integer, String) = Nothing
    ) As ResumenIndice

        If String.IsNullOrWhiteSpace(origen) OrElse Not Directory.Exists(origen) Then
            Throw New DirectoryNotFoundException("La carpeta de origen no existe o no es accesible: " & origen)
        End If

        AsegurarTablaIndice()

        If onProgreso IsNot Nothing Then onProgreso(0, 0, $"Buscando archivos PDF en el origen a alta velocidad ({hilos} núcleos)...")
        Dim archivos = EnumerarArchivosPdfParalelo(origen, token, hilos)
        token.ThrowIfCancellationRequested()

        If archivos.Length = 0 Then
            ' Fallback tradicional si no se encontraron carpetas
            archivos = New DirectoryInfo(origen).EnumerateFiles("*.pdf", SearchOption.AllDirectories).ToArray()
        End If

        ' Seguridad: si no se encontró nada (p. ej. red caída) no se destruye el índice existente.
        If archivos.Length = 0 Then
            Throw New InvalidOperationException("No se encontraron PDFs en el origen; el índice existente no fue modificado.")
        End If

        Dim total As Integer = archivos.Length
        If onProgreso IsNot Nothing Then onProgreso(0, total, "Borrando el índice anterior de esta ruta...")
        BorrarIndiceDirectorio(origen)

        Dim cola As New ConcurrentQueue(Of FilaIndice)()
        Dim finalizadoLectura As Boolean = False
        Dim autoResetEscritor As New AutoResetEvent(False)
        Dim procesados As Integer = 0
        Dim errorEscritura As Exception = Nothing

        ' Tarea asíncrona de fondo dedicada a escribir en MySQL sin bloquear a los hilos de lectura
        Dim tareaEscritora = Task.Run(Sub()
            Try
                Using conn As New MySqlConnection(ModuloConversionJpg.CadenaConexionMysql)
                    conn.Open()
                    While Not finalizadoLectura OrElse Not cola.IsEmpty
                        Dim lote As New List(Of FilaIndice)(TamanoLote)
                        Dim f As FilaIndice = Nothing
                        While lote.Count < TamanoLote AndAlso cola.TryDequeue(f)
                            lote.Add(f)
                        End While

                        If lote.Count > 0 Then
                            InsertarLoteConConexion(conn, lote)
                        ElseIf Not finalizadoLectura Then
                            autoResetEscritor.WaitOne(40)
                        End If
                    End While
                End Using
            Catch exEsc As Exception
                errorEscritura = exEsc
            End Try
        End Sub)

        Dim opciones As New ParallelOptions With {
            .MaxDegreeOfParallelism = Math.Max(1, hilos),
            .CancellationToken = token
        }

        Try
            Parallel.ForEach(archivos, opciones, Sub(fi)
                Dim fila As New FilaIndice With {
                    .Ruta = fi.FullName,
                    .Nombre = fi.Name,
                    .Tamano = fi.Length,
                    .FechaMod = fi.LastWriteTime
                }
                Try
                    fila.Paginas = ModuloConversionJpg.ObtenerTotalPaginasPdf(fi.FullName)
                Catch
                    fila.Paginas = 0
                End Try
                fila.Estatus = If(fila.Paginas > 0, "OK", "ERROR")
                cola.Enqueue(fila)

                If cola.Count >= TamanoLote Then
                    autoResetEscritor.Set()
                End If

                Dim actual = Interlocked.Increment(procesados)
                If onProgreso IsNot Nothing AndAlso (actual Mod 100 = 0 OrElse actual = total) Then
                    onProgreso(actual, total, $"Indexando PDFs a máxima potencia ({hilos} núcleos)...")
                End If
            End Sub)
        Finally
            finalizadoLectura = True
            autoResetEscritor.Set()
            Try
                tareaEscritora.Wait(60000)
            Catch
            End Try
        End Try

        Return ObtenerResumenIndice(origen)
    End Function

    ''' <summary>
    ''' Carga a memoria solo los registros del índice que cuelgan del directorio de origen indicado.
    ''' La llave es la ruta completa del PDF. Devuelve un diccionario vacío si no hay índice.
    ''' </summary>
    Public Function CargarIndiceDirectorio(origen As String) As Dictionary(Of String, InfoIndicePdf)
        Dim dict As New Dictionary(Of String, InfoIndicePdf)(StringComparer.OrdinalIgnoreCase)
        If String.IsNullOrWhiteSpace(origen) Then Return dict
        Try
            Using conn As New MySqlConnection(ModuloConversionJpg.CadenaConexionMysql)
                conn.Open()
                Dim sql = $"SELECT ruta_completa, paginas, tamano, fecha_mod, estatus FROM {NombreTablaIndice} " &
                          "WHERE ruta_completa LIKE @p ESCAPE '|'"
                Using cmd As New MySqlCommand(sql, conn)
                    cmd.CommandTimeout = 300
                    cmd.Parameters.AddWithValue("@p", PatronPrefijo(origen))
                    Using dr = cmd.ExecuteReader()
                        While dr.Read()
                            Dim info As New InfoIndicePdf With {
                                .Paginas = Convert.ToInt32(dr("paginas")),
                                .Tamano = Convert.ToInt64(dr("tamano")),
                                .Estatus = dr("estatus").ToString()
                            }
                            If Not IsDBNull(dr("fecha_mod")) Then info.FechaMod = Convert.ToDateTime(dr("fecha_mod"))
                            Dim rutaComp = dr("ruta_completa").ToString()
                            dict(rutaComp) = info
                            Dim nomArch = Path.GetFileName(rutaComp)
                            If Not String.IsNullOrEmpty(nomArch) AndAlso Not dict.ContainsKey(nomArch) Then
                                dict(nomArch) = info
                            End If
                        End While
                    End Using
                End Using
            End Using
        Catch exTbl As MySqlException When exTbl.Number = 1146
            ' Sin tabla = sin índice.
        Catch ex As Exception
            Debug.WriteLine("Error al cargar el índice de origen: " & ex.Message)
        End Try
        Return dict
    End Function

    ''' <summary>
    ''' Páginas esperadas según el índice para un PDF, o 0 si no está indexado, no se pudo leer
    ''' o el archivo cambió (tamaño/fecha distintos) desde que se indexó.
    ''' </summary>
    Public Function ObtenerPaginasIndexadas(indice As Dictionary(Of String, InfoIndicePdf), archivo As FileInfo) As Integer
        If indice Is Nothing OrElse archivo Is Nothing Then Return 0
        Dim info As InfoIndicePdf = Nothing
        If Not indice.TryGetValue(archivo.FullName, info) Then Return 0
        If Not String.Equals(info.Estatus, "OK", StringComparison.OrdinalIgnoreCase) OrElse info.Paginas <= 0 Then Return 0
        If info.Tamano <> archivo.Length Then Return 0
        If info.FechaMod.HasValue AndAlso Math.Abs((archivo.LastWriteTime - info.FechaMod.Value).TotalSeconds) > 3 Then Return 0
        Return info.Paginas
    End Function

End Module
