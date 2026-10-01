Imports System.IO
Imports ClosedXML.Excel
Imports DocumentFormat.OpenXml.EMMA
Imports DocumentFormat.OpenXml.Wordprocessing
Imports ImageMagick
Imports MySqlConnector
Imports Newtonsoft.Json

Public Class FormLotes

    Private rutas As String() = {
        "\\172.40.5.84\irec",
        "\\172.40.5.84\cats",
        "\\172.40.5.84\rciv"
    }

    Private rutasDestino As String() = {
        "\\172.40.5.84\ssdirec"
    }

    Private rutaDestinoActual As String = ""

    'GLOBALES
    Private rutaActual As String = ""

    Private listaArchivos As New List(Of String)
    Private indiceActual As Integer = 0
    Private Const TAMANO_PAGINA As Integer = 100

    Private cargaToken As Integer = 0
    Private estados As New Dictionary(Of String, EstadoCarpeta)
    Private progresoGlobal As Integer = 0
    Private totalGlobal As Integer = 0
    Private inicioProceso As DateTime

    Private excelBitacora As ExportadorExcelBitacora
    Private rutaExcel As String
    Private lockExcel As New Object()
    Private lockEstado As New Object()
    Private lockMover As New Object()

    Private cancelSourceLotes As Threading.CancellationTokenSource = Nothing
    Private canceladoPorUsuario As Boolean = False
    Private estaProcesando As Boolean = False

    Dim imagenPath As String = Nothing

    'Dim connectionString As String = "server=127.0.0.1;user id=appuser;password=1234;database=stellumdb;"
    Dim connectionString As String = "server=127.0.0.1;port=3307;user id=appuser;password=1234;database=stellumdb;"


    Private db As New BitacoraDB(connectionString)



    Private Sub FormLotes_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        If Not db.ProbarConexion() Then
            MessageBox.Show(
            "No se pudo conectar a la base de datos." & vbCrLf &
            "Se usará modo local (Excel).",
            "Conexión MySQL",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        )
        Else
            MessageBox.Show(
            "Conexión a MySQL exitosa.",
            "Conexión MySQL",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        )
        End If

        CargarDelegaciones()
        ListViewArchivos.Columns.Clear()
        ListViewArchivos.Columns.Add("Nombre", 250)
        ListViewArchivos.Columns.Add("Tamaño", 100)
        ListViewArchivos.Columns.Add("Ruta", 0) ' oculta pero útil
        ListViewArchivos.Sorting = SortOrder.Ascending

        Dim estado As New EstadoCarpeta With {
            .Ruta = rutaActual,
            .Total = listaArchivos.Count,
            .Procesados = 0,
            .Entregable = 0,
            .Renombrar = 0
        }

        estados(rutaActual) = estado

        For Each ruta In rutas
            Try
                If IO.Directory.Exists(ruta) Then

                    Dim item As New RutaItem With {
                .Ruta = ruta,
                .Disponible = True
            }

                    ListBox1.Items.Add(item)

                Else
                    Dim item As New RutaItem With {
                        .Ruta = ruta,
                        .Disponible = False
                    }

                    ListBox1.Items.Add(item)
                End If

            Catch ex As Exception
                ListBox1.Items.Add(ruta & " (Error)")
            End Try
        Next

    End Sub

    Public Class RutaItem
        Public Property Ruta As String
        Public Property Disponible As Boolean

        Public Overrides Function ToString() As String
            If Disponible Then
                Return Ruta
            Else
                Return Ruta & " (No disponible)"
            End If
        End Function
    End Class

    Public Class ResultadoMovimiento
        Public Property Estado As String
        Public Property Tipo As String ' 👈 NUEVO (ENTREGABLE, RENOMBRE, DUPLICADO)
        Public Property RutaDestino As String
    End Class



    Public Class BitacoraDB

        Private connectionString As String

        Public Sub New(conn As String)
            connectionString = conn
        End Sub

        Public Function ProbarConexion() As Boolean
            Try
                Using conn As New MySqlConnection(connectionString)
                    conn.Open()
                    Return True
                End Using
            Catch ex As Exception
                MessageBox.Show("Error real: " & ex.Message)
                Return False
            End Try
        End Function

        Public Sub Insertar(
            disk As String,
            carpetaOrigen As String,
            carpetaDestino As String,
            documento As String,
            nombreOriginal As String,
            nombreNuevo As String,
            comentarios As String,
            errorMsg As String,
            estatus As String,
            tipoArchivo As String, ' 👈 NUEVO
            validado As Integer,
            paginas As Integer,
            usuario As String
        )

            Using conn As New MySqlConnection(connectionString)
                conn.Open()

                Dim query As String = "
                INSERT INTO bitacora
                (disk, carpeta_origen, carpeta_destino, documento, nombre_original, nombre_nuevo,
                 comentarios, error, estatus, tipo_archivo, validado, paginas, usuario)
                VALUES
                (@disk, @origen, @destino, @doc, @nomOrig, @nomNuevo,
                 @coment, @error, @estatus, @tipo, @validado, @paginas, @usuario)
            "

                Using cmd As New MySqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@disk", disk)
                    cmd.Parameters.AddWithValue("@origen", carpetaOrigen)
                    cmd.Parameters.AddWithValue("@destino", carpetaDestino)
                    cmd.Parameters.AddWithValue("@doc", documento)
                    cmd.Parameters.AddWithValue("@nomOrig", nombreOriginal)
                    cmd.Parameters.AddWithValue("@nomNuevo", nombreNuevo)
                    cmd.Parameters.AddWithValue("@coment", comentarios)
                    cmd.Parameters.AddWithValue("@error", errorMsg)
                    cmd.Parameters.AddWithValue("@estatus", estatus)
                    cmd.Parameters.AddWithValue("@tipo", tipoArchivo)
                    cmd.Parameters.AddWithValue("@validado", validado)
                    cmd.Parameters.AddWithValue("@paginas", paginas)
                    cmd.Parameters.AddWithValue("@usuario", usuario)

                    cmd.ExecuteNonQuery()
                End Using
            End Using

        End Sub

    End Class

    Private Sub ListBox1_DoubleClick(sender As Object, e As EventArgs) Handles ListBox1.DoubleClick

        If ListBox1.SelectedItem Is Nothing Then Exit Sub

        Dim ruta = ListBox1.SelectedItem.ToString().Replace(" (No disponible)", "")

        If IO.Directory.Exists(ruta) Then
            Dim psi As New ProcessStartInfo()
            psi.FileName = "explorer.exe"
            psi.Arguments = ruta
            psi.UseShellExecute = True
            Process.Start(psi)
        Else
            MessageBox.Show("Ruta no disponible")
        End If

    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged

        If ListBox1.SelectedItem Is Nothing Then Exit Sub

        Dim item = CType(ListBox1.SelectedItem, RutaItem)

        If Not item.Disponible Then
            MessageBox.Show("Ruta no disponible")
            Exit Sub
        End If

        rutaActual = item.Ruta

        rutaActual = LimpiarRuta(rutaActual)

        'MessageBox.Show("[" & rutaActual & "]")

        If Not IO.Directory.Exists(rutaActual) Then Exit Sub

        ActualizarVista()


    End Sub

    Private Function LimpiarRuta(ruta As String) As String
        Return ruta.Replace(vbCr, "") _
               .Replace(vbLf, "") _
               .Replace(vbTab, "") _
               .Trim()
    End Function


    Private Sub CargarCarpetas(ruta As String)

        ListViewCarpetas.Items.Clear()

        Try
            ' 🔙 Subir nivel
            If IO.Directory.GetParent(ruta) IsNot Nothing Then
                Dim itemUp As New ListViewItem("..")
                itemUp.SubItems.Add("Subir")
                ListViewCarpetas.Items.Add(itemUp)
            End If

            ' 📁 Carpetas
            For Each carpeta In IO.Directory.GetDirectories(ruta)
                Dim item As New ListViewItem(IO.Path.GetFileName(carpeta))
                item.SubItems.Add("Carpeta")
                ListViewCarpetas.Items.Add(item)
            Next

        Catch ex As Exception
            MessageBox.Show("Error al cargar carpetas: " & ex.Message)
        End Try

    End Sub

    Private Sub ListViewCarpetas_DoubleClick(sender As Object, e As EventArgs) Handles ListViewCarpetas.DoubleClick

        If ListViewCarpetas.SelectedItems.Count = 0 Then Exit Sub

        'Dim nombre = ListViewCarpetas.SelectedItems(0).Text
        Dim nombre = LimpiarRuta(ListViewCarpetas.SelectedItems(0).Text)

        ' 🔥 Evita abrir cosas raras
        If String.IsNullOrWhiteSpace(nombre) Then Exit Sub

        If nombre = ".." Then

            ' 🔒 Evita salirte de las rutas base
            If rutas.Any(Function(r) String.Equals(r.TrimEnd("\"c), rutaActual.TrimEnd("\"c), StringComparison.OrdinalIgnoreCase)) Then
                Exit Sub ' ya estás en raíz, no subas más
            End If

            Try
                Dim parent = IO.Directory.GetParent(rutaActual)

                If parent IsNot Nothing Then
                    rutaActual = parent.FullName
                End If

            Catch ex As Exception
                Exit Sub ' evita crash en rutas UNC
            End Try

        Else
            Dim nuevaRuta = IO.Path.Combine(rutaActual, nombre)

            If Not IO.Directory.Exists(nuevaRuta) Then Exit Sub

            rutaActual = nuevaRuta
        End If

        CargarCarpetas(rutaActual)
        CargarArchivosPDF(rutaActual)

        ActualizarVista()

    End Sub

    Private Async Sub CargarArchivosPDF(ruta As String)
        cargaToken += 1
        Dim miToken = cargaToken

        ListViewArchivos.Items.Clear()
        listaArchivos.Clear()
        indiceActual = 0

        Dim totalBytes As Long = 0
        Dim cantidad As Integer = 0

        Await Task.Run(Sub()

                           For Each archivo In IO.Directory.EnumerateFiles(ruta, "*.pdf")
                               If miToken <> cargaToken Then Exit Sub
                               Try
                                   listaArchivos.Add(archivo)

                                   Dim info As New IO.FileInfo(archivo)
                                   totalBytes += info.Length
                                   cantidad += 1

                               Catch
                               End Try
                           Next

                       End Sub)

        ' 🔥 Mostrar PRIMEROS 100

        If miToken <> cargaToken Then Exit Sub

        CargarSiguientePagina()

        Dim totalMB = totalBytes / 1024 / 1024
        lblResumen.Text = $"Archivos: {cantidad} | Tamaño: {totalMB:0.00} MB"
        btnProcesar.Enabled = (cantidad > 0)

    End Sub

    Private Sub CargarSiguientePagina()

        ListViewArchivos.BeginUpdate()

        Dim limite = Math.Min(indiceActual + TAMANO_PAGINA, listaArchivos.Count)

        For i = indiceActual To limite - 1

            Dim archivo = listaArchivos(i)
            Dim info As New IO.FileInfo(archivo)

            Dim sizeText As String

            If info.Length >= 1024L * 1024 * 1024 Then
                sizeText = $"{info.Length / 1024 / 1024 / 1024:0.00} GB"
            ElseIf info.Length >= 1024L * 1024 Then
                sizeText = $"{info.Length / 1024 / 1024:0.00} MB"
            Else
                sizeText = $"{info.Length / 1024:0.00} KB"
            End If

            Dim item As New ListViewItem(IO.Path.GetFileName(archivo))
            item.SubItems.Add(sizeText)
            item.SubItems.Add(archivo)

            ListViewArchivos.Items.Add(item)

        Next

        indiceActual = limite

        ListViewArchivos.EndUpdate()

    End Sub

    Private Sub ListViewArchivos_MouseWheel(sender As Object, e As MouseEventArgs) Handles ListViewArchivos.MouseWheel

        If ListViewArchivos.Items.Count = 0 Then Exit Sub

        Dim ultimoVisible = ListViewArchivos.TopItem.Index + ListViewArchivos.ClientSize.Height \ ListViewArchivos.Items(0).Bounds.Height

        If ultimoVisible >= ListViewArchivos.Items.Count - 10 Then
            If indiceActual < listaArchivos.Count Then
                CargarSiguientePagina()
            End If
        End If

    End Sub

    Private Sub ListViewCarpetas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListViewCarpetas.SelectedIndexChanged

    End Sub

    Private Sub ListViewPDF_SelectedIndexChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub ListViewArchivos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListViewArchivos.SelectedIndexChanged

        If ListViewArchivos.SelectedItems.Count = 0 Then Exit Sub

        Dim item = ListViewArchivos.SelectedItems(0)
        Dim rutaPDF = item.SubItems(2).Text

        If Not IO.File.Exists(rutaPDF) Then Exit Sub

        WebViewPDF.Source = New Uri(rutaPDF)

    End Sub

    Private Function ObtenerRutaRelativa(rutaCompleta As String) As String

        For Each raiz In rutas

            Dim raizNormalizada = raiz.TrimEnd("\"c)

            If rutaCompleta.StartsWith(raizNormalizada, StringComparison.OrdinalIgnoreCase) Then

                Dim relativa = rutaCompleta.Substring(raizNormalizada.Length)

                relativa = relativa.TrimStart("\"c)

                Dim nombreRaiz = IO.Path.GetFileName(raizNormalizada)

                If String.IsNullOrEmpty(nombreRaiz) Then
                    nombreRaiz = raizNormalizada ' fallback si algo raro pasa
                End If

                Return nombreRaiz & If(relativa <> "", "\" & relativa, "")

            End If
        Next

        Return rutaCompleta ' fallback

    End Function

    Private Sub ActualizarVista()

        CargarCarpetas(rutaActual)
        CargarArchivosPDF(rutaActual)
        'LabelRuta.Text = ObtenerRutaRelativa(rutaActual).Replace("\", " / ")
        LabelRuta.Text = "📂 " & ObtenerRutaRelativa(rutaActual).Replace("\", " / ")

    End Sub


    Private semaphore As Threading.SemaphoreSlim = Nothing

    Private Sub btnConfigRecursosLotes_Click(sender As Object, e As EventArgs) Handles btnConfigRecursosLotes.Click
        Dim frm As New FormConfigRecursos()
        If frm.ShowDialog(Me) = DialogResult.OK Then
            ModuloRecursos.AplicarLimitesMagickNET()
            Dim nuevosHilos = ModuloRecursos.ObtenerHilosOptimos()
            semaphore = New Threading.SemaphoreSlim(nuevosHilos)
            MessageBox.Show($"Configuración aplicada: {nuevosHilos} hilos concurrentes activos.", "Rendimiento", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Public Async Function ProcesarLote(ruta As String) As Task

        Dim hilosOptimos = ModuloRecursos.ObtenerHilosOptimos()
        If semaphore Is Nothing Then
            semaphore = New Threading.SemaphoreSlim(hilosOptimos)
        End If

        Dim rutaBase As String = ruta
        Console.WriteLine(rutaBase)

        Dim dirInfo As New IO.DirectoryInfo(rutaBase)
        Dim archivos = dirInfo.EnumerateFiles("*.pdf").OrderByDescending(Function(f) f.Length).Select(Function(f) f.FullName).ToArray()

        Dim tasks As New List(Of Task)

        For Each archivo In archivos
            If canceladoPorUsuario OrElse (cancelSourceLotes IsNot Nothing AndAlso cancelSourceLotes.IsCancellationRequested) Then
                Exit For
            End If

            Await semaphore.WaitAsync()
            Dim excelLocal = excelBitacora ' 👈 congelas referencia

            tasks.Add(Task.Run(Sub()
                                   Try
                                       If canceladoPorUsuario OrElse (cancelSourceLotes IsNot Nothing AndAlso cancelSourceLotes.IsCancellationRequested) Then
                                           Exit Sub
                                       End If
                                       ProcesarArchivo(archivo, excelLocal, If(cancelSourceLotes IsNot Nothing, cancelSourceLotes.Token, Threading.CancellationToken.None))
                                   Finally
                                       semaphore.Release()
                                   End Try
                               End Sub))
        Next

        Await Task.WhenAll(tasks)

    End Function


    Private Sub ProcesarArchivo(rutaPDF As String, excel As ExportadorExcelBitacora, Optional token As Threading.CancellationToken = Nothing)
        If canceladoPorUsuario OrElse (token <> Nothing AndAlso token.IsCancellationRequested) Then
            Exit Sub
        End If

        Console.WriteLine($"Iniciando archivo: {rutaPDF}")
        Logger.Info($"Iniciando archivo: {rutaPDF}")

        Dim imagenTemp As String = Nothing

        Try
            ' 1. Validar header PDF
            Using fs = New IO.FileStream(rutaPDF, IO.FileMode.Open, IO.FileAccess.Read)
                Dim buffer(4) As Byte
                fs.Read(buffer, 0, 5)
                Dim header = System.Text.Encoding.ASCII.GetString(buffer)

                If header <> "%PDF-" Then
                    Throw New Exception("Archivo no es PDF válido")
                End If

                Console.WriteLine("Archivo válido")
            End Using

            If canceladoPorUsuario OrElse (token <> Nothing AndAlso token.IsCancellationRequested) Then
                Exit Sub
            End If

            ' 1.5 Obtener número de páginas
            Dim totalPaginas As Integer = 0

            Try
                Using collection As New ImageMagick.MagickImageCollection()
                    collection.Ping(rutaPDF) ' 👈 solo metadata (rápido)
                    totalPaginas = collection.Count
                End Using

            Catch ex As Exception
                Logger.ErrorLog("Error obteniendo páginas: " & ex.Message)
                totalPaginas = 0
            End Try

            If totalPaginas <= 0 Then
                Throw New InvalidDataException("El archivo PDF no contiene páginas legibles o está dañado.")
            End If

            If canceladoPorUsuario OrElse (token <> Nothing AndAlso token.IsCancellationRequested) Then
                Exit Sub
            End If

            Try
                ' 2. Convertir primera página a imagen
                imagenTemp = ConvertirPrimeraPaginaAImagen(rutaPDF)

                If canceladoPorUsuario OrElse (token <> Nothing AndAlso token.IsCancellationRequested) Then
                    Exit Sub
                End If

                ' 3. Detectar orientación
                Dim orientacion = DetectarOrientacion(imagenTemp)

                If orientacion.Angulo <> 0 AndAlso orientacion.Confianza > 5 Then
                    RotarImagen(imagenTemp, orientacion.Angulo)
                    Console.WriteLine("Se rotó la imagen")
                End If

                If canceladoPorUsuario OrElse (token <> Nothing AndAlso token.IsCancellationRequested) Then
                    Exit Sub
                End If

                ' 5. OCR
                Dim texto = EjecutarOCR(imagenTemp)
                Console.WriteLine(texto)

                ' 6. Extraer datos
                Dim datos = ExtraerDatosOCR(texto)
                Console.WriteLine(datos)

                If datos IsNot Nothing Then
                    Logger.Info($"OCR OK: {datos.Registro} - {datos.Anio}")
                Else
                    Logger.Info("OCR sin datos")
                End If

                ' 7. Validar nombre
                Dim resultado = AnalizarArchivo(IO.Path.GetFileName(rutaPDF), datos)

                Dim usuario = Environment.UserName
                Dim disk = ObtenerRoot(rutaActual)

                ' Chequeo crítico de cancelación antes de alterar disco o mover archivo
                If canceladoPorUsuario OrElse (token <> Nothing AndAlso token.IsCancellationRequested) Then
                    Exit Sub
                End If

                ' 8. Mover archivo
                Dim estadoMovimiento = MoverArchivo(rutaPDF, resultado)

                SyncLock lockEstado
                    Dim estado As EstadoCarpeta = Nothing

                    If Not estados.TryGetValue(rutaActual, estado) Then
                        estado = New EstadoCarpeta With {
                            .Ruta = rutaActual,
                            .Total = 0
                        }
                        estados(rutaActual) = estado
                    End If

                    estado.Procesados += 1

                    If resultado.Valido Then
                        estado.Entregable += 1
                    Else
                        estado.Renombrar += 1
                    End If
                End SyncLock

                Dim registroTexto As String = ""
                If datos IsNot Nothing Then
                    registroTexto = datos.Registro & " - " & datos.Anio
                End If

                If excel Is Nothing Then
                    Throw New Exception("excelBitacora no inicializado")
                End If

                ' Desacoplar inserción en base de datos mediante cola asíncrona de alto rendimiento (sin bloqueos)
                ModuloBitacoraAsync.EncolarLote(
                    disk,
                    rutaActual,
                    estadoMovimiento.RutaDestino,
                    rutaPDF,
                    IO.Path.GetFileName(rutaPDF),
                    "",
                    registroTexto,
                    If(resultado.Valido, "", "OCR o validación fallida"),
                    resultado.RutaBase,
                    estadoMovimiento.Tipo,
                    If(estadoMovimiento.Tipo = "DUPLICADO", 2, If(resultado.Valido, 1, 0)),
                    totalPaginas,
                    usuario
                )
            Finally
                ' 🧹 LIMPIEZA DEL TEMPORAL
                If Not String.IsNullOrEmpty(imagenTemp) AndAlso IO.File.Exists(imagenTemp) Then
                    Try
                        IO.File.Delete(imagenTemp)
                    Catch ex As Exception
                        Logger.Info($"No se pudo borrar temporal: {imagenTemp} - {ex.Message}")
                    End Try
                End If
            End Try

        Catch ex As Exception
            Logger.ErrorLog($"Error en archivo {rutaPDF}: {ex.Message}")
            Logger.ErrorLog(ex.StackTrace)
        End Try

    End Sub

    Private Function EjecutarOCR(imagenPath As String) As String

        Dim psi As New ProcessStartInfo()
        psi.FileName = "tesseract"

        psi.Arguments = $"""{imagenPath}"" stdout -l eng --oem 3 --psm 6 " &
                    "-c tessedit_char_whitelist=ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789: " &
                    "-c preserve_interword_spaces=1"

        psi.RedirectStandardOutput = True
        psi.RedirectStandardError = True
        psi.UseShellExecute = False
        psi.CreateNoWindow = True

        Dim p As Process = Process.Start(psi)

        Dim output = p.StandardOutput.ReadToEnd()
        Dim err = p.StandardError.ReadToEnd()

        p.WaitForExit()

        ' 🔥 Debug (igual que Log::info)
        ' Console.WriteLine("OCR CRUDO:")
        ' Console.WriteLine(output)

        Return output

    End Function

    Private Function DetectarOrientacion(imagenPath As String) As ResultadoOrientacion

        Dim psi As New ProcessStartInfo()
        psi.FileName = "tesseract"
        psi.Arguments = $"""{imagenPath}"" stdout --psm 0"
        psi.RedirectStandardOutput = True
        psi.UseShellExecute = False
        psi.CreateNoWindow = True

        Dim p = Process.Start(psi)
        Dim output = p.StandardOutput.ReadToEnd()
        p.WaitForExit()

        Dim resultado As New ResultadoOrientacion()

        Dim matchA = System.Text.RegularExpressions.Regex.Match(output, "Rotate: (\d+)")
        If matchA.Success Then resultado.Angulo = CInt(matchA.Groups(1).Value)

        Dim matchC = System.Text.RegularExpressions.Regex.Match(output, "Orientation confidence: ([\d\.]+)")
        If matchC.Success Then resultado.Confianza = CDbl(matchC.Groups(1).Value)

        Return resultado

    End Function

    Private Function ConvertirPrimeraPaginaAImagen(pdfPath As String) As String

        'Dim output = IO.Path.GetTempFileName() & ".jpg"
        Dim output = IO.Path.Combine(
            IO.Path.GetTempPath(),
            Guid.NewGuid().ToString() & ".jpg"
        )

        Using images As New MagickImageCollection()
            Dim settings As New MagickReadSettings()
            settings.Density = New Density(200)

            images.Read(pdfPath & "[0]", settings)

            Using img = images(0)
                img.Format = MagickFormat.Jpeg
                img.Write(output)
            End Using
        End Using

        Return output

    End Function

    Private Function ValidarNombre(rutaPDF As String, datos As DatosOCR) As Boolean

        Dim nombre = IO.Path.GetFileNameWithoutExtension(rutaPDF)
        Dim partes = nombre.Split("_"c)


        Dim claveMuni = partes(0)
        Dim anioArchivo = partes(1)
        Dim registroArchivo = partes(2)


        Console.WriteLine($"Nombre original: {nombre}")
        Console.WriteLine("Municipio al hacer split: ", claveMuni)
        Console.WriteLine("Año al hacer split: ", anioArchivo)
        Console.WriteLine("Registro al hacer split: ", registroArchivo)

        Console.WriteLine("Datos obtenidos por OCR: ")

        Console.WriteLine($"Registro pdf ocr: {datos.Registro}")
        Console.WriteLine($"Registro pdf ocr: {datos.Anio}")

        Return (datos.Registro = registroArchivo AndAlso datos.Anio = anioArchivo)



    End Function

    'Private Sub MoverArchivo(origen As String, resultado As ResultadoAnalisis)

    'Dim nombre = IO.Path.GetFileName(origen)

    'Dim root = ObtenerRoot(rutaActual)

    'Dim destinoFinal = IO.Path.Combine(root, resultado.RutaBase, nombre)

    'Dim carpeta = IO.Path.GetDirectoryName(destinoFinal)

    'If Not IO.Directory.Exists(carpeta) Then
    'IO.Directory.CreateDirectory(carpeta)
    'End If

    'IO.File.Move(origen, destinoFinal)

    'Logger.Info($"MOVIDO: {destinoFinal}")
    'Console.WriteLine($"El archivo se movió a {destinoFinal}")

    'End Sub

    Private Function MoverArchivo(origen As String, resultado As ResultadoAnalisis) As ResultadoMovimiento

        Dim nombre = IO.Path.GetFileName(origen)
        Dim root = ObtenerRoot(rutaActual)

        Dim destinoFinal = IO.Path.Combine(root, resultado.RutaBase, nombre)
        Dim carpetaDestino = IO.Path.GetDirectoryName(destinoFinal)

        If Not IO.Directory.Exists(carpetaDestino) Then
            IO.Directory.CreateDirectory(carpetaDestino)
        End If

        SyncLock lockMover

            If IO.File.Exists(destinoFinal) Then

                Dim carpetaDuplicados = IO.Path.Combine(root, "Duplicados")

                If Not IO.Directory.Exists(carpetaDuplicados) Then
                    IO.Directory.CreateDirectory(carpetaDuplicados)
                End If

                Dim destinoDuplicado = IO.Path.Combine(carpetaDuplicados, nombre)

                Dim contador As Integer = 1
                Dim nombreBase = IO.Path.GetFileNameWithoutExtension(nombre)
                Dim extension = IO.Path.GetExtension(nombre)

                While IO.File.Exists(destinoDuplicado)
                    destinoDuplicado = IO.Path.Combine(
                    carpetaDuplicados,
                    $"{nombreBase}_dup{contador}{extension}"
                )
                    contador += 1
                End While

                IO.File.Move(origen, destinoDuplicado)

                ' Verificación física del archivo movido
                Dim fiDup As New IO.FileInfo(destinoDuplicado)
                If Not fiDup.Exists OrElse fiDup.Length = 0 Then
                    Throw New IO.IOException($"Error de integridad física: El archivo movido a {destinoDuplicado} está vacío o no existe.")
                End If

                Logger.Info($"DUPLICADO: {destinoDuplicado}")

                Return New ResultadoMovimiento With {
                .Tipo = "DUPLICADO",
                .Estado = "Duplicado",
                .RutaDestino = "Duplicados"
            }
            Else

                IO.File.Move(origen, destinoFinal)

                ' Verificación física del archivo movido
                Dim fiFinal As New IO.FileInfo(destinoFinal)
                If Not fiFinal.Exists OrElse fiFinal.Length = 0 Then
                    Throw New IO.IOException($"Error de integridad física: El archivo movido a {destinoFinal} está vacío o no existe.")
                End If

                Dim tipo As String = If(resultado.Valido, "ENTREGABLE", "RENOMBRE")

                Logger.Info($"MOVIDO: {destinoFinal}")

                Return New ResultadoMovimiento With {
                .Tipo = tipo,
                .Estado = "Entregable",
                .RutaDestino = resultado.RutaBase
            }

            End If

        End SyncLock

    End Function

    Private Function ExtraerDatosOCR(textoOCR As String) As DatosOCR

        If String.IsNullOrWhiteSpace(textoOCR) Then Return Nothing

        Dim texto As String = textoOCR.ToLower()

        ' 🔤 Normalizar acentos
        texto = texto.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")

        ' 🔥 Separar letras y números pegados
        texto = System.Text.RegularExpressions.Regex.Replace(texto, "([a-z])([0-9])", "$1 $2")
        texto = System.Text.RegularExpressions.Regex.Replace(texto, "([0-9])([a-z])", "$1 $2")

        ' 🔥 Separar palabras clave pegadas
        texto = texto.Replace("fechaderegistro", "fecha de registro") _
                     .Replace("fecha", "fecha") _
                     .Replace("registro", "registro") _
                     .Replace("registre", "registro") _
                     .Replace("oficinaregistralde", "oficina registral de") _
                     .Replace("municipio", "municipio")

        ' 🔄 Limpiar espacios múltiples
        texto = System.Text.RegularExpressions.Regex.Replace(texto, "\s+", " ")

        Dim datos As New DatosOCR()

        ' =========================
        ' 1. REGISTRO
        ' =========================
        Dim matchRegistro = System.Text.RegularExpressions.Regex.Match(texto, "registro[:\s]*([0-9]{1,7})")

        If matchRegistro.Success Then
            datos.Registro = matchRegistro.Groups(1).Value
        Else
            Dim fallback = System.Text.RegularExpressions.Regex.Match(texto, "registro[^0-9]*([0-9]{1,7})")
            If fallback.Success Then
                datos.Registro = fallback.Groups(1).Value
            End If
        End If

        ' =========================
        ' 2. FECHA TEXTO
        ' =========================
        Dim matchFecha = System.Text.RegularExpressions.Regex.Match(texto, "(\d{1,2}\s+de\s+[a-z]+\s+(del\s+)?\d{4})")
        If matchFecha.Success Then
            datos.Fecha = matchFecha.Groups(1).Value
        End If

        ' =========================
        ' 3. AÑO
        ' =========================
        Dim matchAnio = System.Text.RegularExpressions.Regex.Match(texto, "\b(20\d{2})\b")
        If matchAnio.Success Then
            datos.Anio = matchAnio.Groups(1).Value
        End If

        ' =========================
        ' 4. MUNICIPIO
        ' =========================
        Dim matchMunicipio = System.Text.RegularExpressions.Regex.Match(
            texto,
            "oficina registral de ([a-z\s]+?)(?: fecha| registro| esta|$)"
        )

        If matchMunicipio.Success Then
            Dim municipio = matchMunicipio.Groups(1).Value

            municipio = municipio.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            municipio = System.Text.RegularExpressions.Regex.Replace(municipio, "[^a-z\s]", "")
            municipio = municipio.Trim()

            datos.Municipio = municipio
        End If

        Return datos

    End Function

    Private Sub RotarImagen(rutaImagen As String, angulo As Integer)

        Using img As New MagickImage(rutaImagen)

            Select Case angulo
                Case 90
                    img.Rotate(90)
                Case 180
                    img.Rotate(180)
                Case 270
                    img.Rotate(270)
            End Select

            img.Write(rutaImagen)

        End Using

    End Sub

    Public Class DatosOCR
        Public Property Registro As String
        Public Property Fecha As String
        Public Property Anio As String
        Public Property Municipio As String
        Public Property ClaveMunicipio As String
    End Class

    Public Class ResultadoOrientacion
        Public Property Angulo As Integer
        Public Property Confianza As Double
    End Class

    Public Class EstadoCarpeta
        Public Property Ruta As String
        Public Property Total As Integer
        Public Property Procesados As Integer
        Public Property Entregable As Integer
        Public Property Renombrar As Integer

        Public ReadOnly Property Progreso As Double
            Get
                If Total = 0 Then Return 0
                Return (Procesados / Total) * 100
            End Get
        End Property
    End Class

    Private Sub FormLotes_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If estaProcesando Then
            Dim resp = MessageBox.Show(
                "Hay un proceso de análisis por lotes en ejecución." & vbCrLf &
                "¿Desea cancelar el proceso y salir?" & vbCrLf & vbCrLf &
                "⚠️ Los archivos que no hayan completado el proceso no se moverán ni se registrarán en la base de datos.",
                "Confirmar Salida y Cancelación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning)

            If resp = DialogResult.Yes Then
                CancelarLotesYLimpiar()
                Dim sw = Diagnostics.Stopwatch.StartNew()
                While estaProcesando AndAlso sw.ElapsedMilliseconds < 3500
                    Application.DoEvents()
                    Threading.Thread.Sleep(100)
                End While
            Else
                e.Cancel = True
            End If
        End If
    End Sub

    Private Sub btnCancelarLotes_Click(sender As Object, e As EventArgs) Handles btnCancelarLotes.Click
        If estaProcesando Then
            Dim resp = MessageBox.Show(
                "¿Desea cancelar el procesamiento por lotes?" & vbCrLf & vbCrLf &
                "⚠️ Los archivos que no se hayan completado se descartarán de inmediato y no se registrarán en la base de datos.",
                "Confirmar Cancelación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If resp = DialogResult.Yes Then
                btnCancelarLotes.Enabled = False
                btnCancelarLotes.Text = "Cancelando..."
                CancelarLotesYLimpiar()
            End If
        End If
    End Sub

    Private Sub CancelarLotesYLimpiar()
        canceladoPorUsuario = True
        If cancelSourceLotes IsNot Nothing AndAlso Not cancelSourceLotes.IsCancellationRequested Then
            cancelSourceLotes.Cancel()
        End If
        Logger.Info("CANCELACIÓN SOLICITADA POR EL USUARIO EN LOTES")
    End Sub

    Private Async Sub btnProcesar_Click(sender As Object, e As EventArgs) Handles btnProcesar.Click
        Dim root As String = ObtenerRoot(rutaActual)

        Try
            btnProcesar.Enabled = False
            btnCancelarLotes.Enabled = True
            btnCancelarLotes.Text = "Cancelar"
            canceladoPorUsuario = False
            cancelSourceLotes = New Threading.CancellationTokenSource()
            estaProcesando = True

            estados.Clear()
            progresoGlobal = 0
            totalGlobal = listaArchivos.Count
            inicioProceso = DateTime.Now

            estados(rutaActual) = New EstadoCarpeta With {
                .Ruta = rutaActual,
                .Total = listaArchivos.Count,
                .Procesados = 0,
                .Entregable = 0,
                .Renombrar = 0
            }

            ' 🔥 CREA EL EXCEL ANTES
            Dim semana = $"{DateTime.Now:yyyy}_W{Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(DateTime.Now, Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)}"
            Dim dir = "C:\Reportes"

            If Not IO.Directory.Exists(dir) Then
                IO.Directory.CreateDirectory(dir)
            End If

            rutaExcel = IO.Path.Combine(dir, $"bitacora_{semana}.xlsx")
            excelBitacora = New ExportadorExcelBitacora(rutaExcel)

            ' 🔥 AHORA SÍ PROCESA
            ModuloBitacoraAsync.IniciarServicioBitacora()
            Await ProcesarLote(rutaActual)

            ' 🔥 GUARDA AL FINAL
            If Not canceladoPorUsuario Then
                excelBitacora.Guardar(rutaExcel)
                ModuloBitacoraAsync.EsperarVaciado(3000)
                MessageBox.Show("Proceso terminado. Bitácora registrada y Excel generado.")
            Else
                ModuloBitacoraAsync.EsperarVaciado(1000)
            End If

        Catch ex As Exception
            MessageBox.Show(ex.Message)

        Finally
            estaProcesando = False
            btnProcesar.Enabled = True
            btnCancelarLotes.Enabled = False
            btnCancelarLotes.Text = "Cancelar"
            If canceladoPorUsuario Then
                MessageBox.Show("El procesamiento de lotes fue cancelado. Los archivos que no se terminaron no fueron movidos ni registrados en la base de datos.", "Cancelación Completada", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Try
    End Sub

    Private Delegaciones As List(Of DelegacionItem)

    Private Sub CargarDelegaciones()
        Try

            Dim ruta = IO.Path.Combine(Application.StartupPath, "config\delegaciones.json")

            If Not IO.File.Exists(ruta) Then
                Throw New Exception("No existe el archivo de delegaciones: " & ruta)
            End If

            Dim json As String = IO.File.ReadAllText(ruta)

            Delegaciones = JsonConvert.DeserializeObject(Of List(Of DelegacionItem))(json)

            If Delegaciones Is Nothing OrElse Delegaciones.Count = 0 Then
                Throw New Exception("JSON de delegaciones vacío o inválido")
            End If

            Logger.Info($"Delegaciones cargadas: {Delegaciones.Count}")

        Catch ex As Exception
            Logger.ErrorLog("Error cargando delegaciones: " & ex.Message)
            Delegaciones = New List(Of DelegacionItem) ' fallback seguro
        End Try
    End Sub

    Private Function AnalizarArchivo(nombreArchivo As String, datos As DatosOCR) As ResultadoAnalisis

        Dim resultado As New ResultadoAnalisis()

        ' =========================
        ' 1. Parse nombre archivo
        ' =========================
        Dim sinExt = IO.Path.GetFileNameWithoutExtension(nombreArchivo)
        Dim partes = sinExt.Split("_"c)

        Dim claveMuni As String = Nothing
        Dim anioArchivo As String = Nothing
        Dim registroArchivo As String = Nothing

        If partes.Length >= 3 Then
            claveMuni = partes(0)
            anioArchivo = partes(1)
            registroArchivo = partes(2)
        End If

        ' =========================
        ' 2. Buscar delegación
        ' =========================
        'Dim delegacion = Delegaciones.FirstOrDefault(Function(d) d.Clave = claveMuni)
        Dim claveMuniClean = If(claveMuni, "").Trim()

        Dim delegacion = Delegaciones.FirstOrDefault(Function(d)
                                                         Return d.Clave.Trim() = claveMuniClean
                                                     End Function)

        resultado.ClaveMunicipio = claveMuni
        resultado.Delegacion = delegacion?.Nombre
        resultado.AnioArchivo = anioArchivo
        resultado.RegistroArchivo = registroArchivo

        ' =========================
        ' 3. Validaciones OCR vs nombre
        ' =========================
        Dim valido As Boolean =
            Not String.IsNullOrWhiteSpace(datos.Registro) AndAlso
            Not String.IsNullOrWhiteSpace(datos.Anio) AndAlso
            datos.Registro = registroArchivo AndAlso
            datos.Anio = anioArchivo AndAlso
            delegacion IsNot Nothing

        resultado.Valido = valido

        ' =========================
        ' 4. Ruta destino lógica (tipo Laravel)
        ' =========================
        Dim base = If(valido, "Entregable", "Renombrar")

        Dim claveFinal = If(delegacion IsNot Nothing, delegacion.Clave, "SIN_CLAVE")
        Dim anioFinal = If(String.IsNullOrWhiteSpace(anioArchivo), "SIN_ANIO", anioArchivo)

        resultado.RutaBase = IO.Path.Combine(base, claveFinal, anioFinal)

        Return resultado

    End Function

    Private Function ObtenerRoot(ruta As String) As String

        If ruta.StartsWith("\\172.40.5.84\irec", StringComparison.OrdinalIgnoreCase) Then
            Return "\\172.40.5.84\irec"

        ElseIf ruta.StartsWith("\\172.40.5.84\cats", StringComparison.OrdinalIgnoreCase) Then
            Return "\\172.40.5.84\cats"

        ElseIf ruta.StartsWith("\\172.40.5.84\rciv", StringComparison.OrdinalIgnoreCase) Then
            Return "\\172.40.5.84\rciv"
        End If

        Throw New Exception("Ruta base no reconocida")
    End Function
End Class

'Fin Procesamiento
Public Class ResultadoAnalisis
    Public Property ClaveMunicipio As String
    Public Property Delegacion As String
    Public Property AnioArchivo As String
    Public Property RegistroArchivo As String
    Public Property Valido As Boolean
    Public Property RutaBase As String
End Class

Public Class ExportadorExcelBitacora

    Private ReadOnly filePath As String
    Private wb As XLWorkbook
    Private ws As IXLWorksheet

    Public Sub New(path As String)
        filePath = path

        If File.Exists(filePath) Then
            wb = New XLWorkbook(filePath)

            If wb.Worksheets.Contains("Bitacora") Then
                ws = wb.Worksheet("Bitacora")
            Else
                ws = wb.Worksheets.Add("Bitacora")
            End If

        Else
            wb = New XLWorkbook()
            ws = wb.Worksheets.Add("Bitacora")

            ' headers
            ws.Cell(1, 1).Value = "Disk"
            ws.Cell(1, 2).Value = "Carpeta Origen"
            ws.Cell(1, 3).Value = "Carpeta Destino"
            ws.Cell(1, 4).Value = "Documento"
            ws.Cell(1, 5).Value = "Nombre Original"
            ws.Cell(1, 6).Value = "Nombre Nuevo"
            ws.Cell(1, 7).Value = "Comentarios"
            ws.Cell(1, 8).Value = "Error"
            ws.Cell(1, 9).Value = "Estatus"
            ws.Cell(1, 10).Value = "Validado"
            ws.Cell(1, 11).Value = "Paginas"
            ws.Cell(1, 12).Value = "Usuario"
            ws.Cell(1, 13).Value = "Fecha"
        End If
    End Sub


    Public Sub AgregarBitacora(
        disk As String,
        carpeta As String,
        carpetaFinal As String,
        documento As String,
        nombreOriginal As String,
        nombreNuevo As String,
        comentarios As String,
        errorMsg As String,
        estatus As String,
        validado As Integer,
        paginas As Integer,
        usuario As String
    )

        'Dim lastRow = ws.LastRowUsed().RowNumber() + 1
        Dim lastRow As Integer = If(ws.LastRowUsed() Is Nothing, 1, ws.LastRowUsed().RowNumber() + 1)
        'Dim lastRow = ws.LastRowUsed()?.RowNumber() + 1
        'If lastRow Is Nothing OrElse lastRow = 1 Then lastRow = 2

        ws.Cell(lastRow, 1).Value = disk
        ws.Cell(lastRow, 2).Value = carpeta
        ws.Cell(lastRow, 3).Value = carpetaFinal
        ws.Cell(lastRow, 4).Value = documento
        ws.Cell(lastRow, 5).Value = nombreOriginal
        ws.Cell(lastRow, 6).Value = nombreNuevo
        ws.Cell(lastRow, 7).Value = comentarios
        ws.Cell(lastRow, 8).Value = errorMsg
        ws.Cell(lastRow, 9).Value = estatus
        ws.Cell(lastRow, 10).Value = validado
        ws.Cell(lastRow, 11).Value = paginas
        ws.Cell(lastRow, 12).Value = usuario
        ws.Cell(lastRow, 13).Value = DateTime.Now

    End Sub


    Public Sub Guardar(path As String)
        wb.SaveAs(path)
    End Sub

End Class

Public Class Logger

    Private Shared ReadOnly lockObj As New Object()
    Private Shared logPath As String = "C:\Logs\procesamiento_log.txt"

    Public Shared Sub Info(msg As String)
        Write("INFO", msg)
    End Sub

    Public Shared Sub ErrorLog(msg As String)
        Write("ERROR", msg)
    End Sub

    Private Shared Sub Write(tipo As String, msg As String)

        SyncLock lockObj

            Dim dir = Path.GetDirectoryName(logPath)

            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            Dim line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{tipo}] {msg}"

            File.AppendAllText(logPath, line & Environment.NewLine)

        End SyncLock

    End Sub

End Class


Public Class DelegacionItem
    Public Property Clave As String
    Public Property Nombre As String
End Class