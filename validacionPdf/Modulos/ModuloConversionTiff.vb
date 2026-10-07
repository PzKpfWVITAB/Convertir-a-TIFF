Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports ImageMagick
Imports iText.Kernel.Pdf
Imports MySqlConnector

''' <summary>
''' Módulo centralizado y optimizado para la conversión de PDF a imágenes TIFF (250 DPI LZW, escala de grises),
''' validación física rigurosa y sincronización masiva con MySQL (bitacora_transferencia_tiff).
''' </summary>
Public Module ModuloConversionTiff

    Public Class InfoRegistroBd
        Public Property Encontrado As Boolean = False
        Public Property Estatus As String = ""
        Public Property Paginas As Integer = 0
        Public Property ErrorMsg As String = ""
    End Class

    ''' <summary>
    ''' Cadena de conexión activa para MySQL en stellumdb.
    ''' </summary>
    Public Property CadenaConexionMysql As String = "server=127.0.0.1;port=3307;user id=appuser;password=1234;database=stellumdb;ConnectionTimeout=5;Pooling=true;MinPoolSize=1;MaxPoolSize=10;"

    Public ReadOnly Property NombreTablaBitacora As String
        Get
            Return "bitacora_transferencia_tiff"
        End Get
    End Property

    ''' <summary>
    ''' Normaliza y extrae la ruta relativa de un archivo respecto al directorio base.
    ''' </summary>
    Public Function ObtenerRutaRelativa(baseDir As String, fullPath As String) As String
        Try
            Dim uriBase As New Uri(If(baseDir.EndsWith("\") OrElse baseDir.EndsWith("/"), baseDir, baseDir & Path.DirectorySeparatorChar))
            Dim uriFile As New Uri(fullPath)
            Dim relUri = uriBase.MakeRelativeUri(uriFile)
            Return Uri.UnescapeDataString(relUri.ToString()).Replace("/"c, Path.DirectorySeparatorChar)
        Catch
            Return Path.GetFileName(fullPath)
        End Try
    End Function

    ''' <summary>
    ''' Limpia los separadores para asegurar consistencia en subdirectorios relativos.
    ''' </summary>
    Public Function NormalizarRutaRelativa(relDir As String) As String
        If String.IsNullOrEmpty(relDir) Then Return ""
        Dim normalizado = relDir.Replace("/"c, Path.DirectorySeparatorChar).Trim(Path.DirectorySeparatorChar)
        If normalizado = "." Then Return ""
        Return normalizado
    End Function

    ''' <summary>
    ''' Elimina completamente una carpeta en destino que haya quedado incompleta, vacía o corrupta.
    ''' </summary>
    Public Sub LimpiarCarpetaIncompleta(carpeta As String)
        Try
            If String.IsNullOrWhiteSpace(carpeta) Then Exit Sub
            If Directory.Exists(carpeta) Then
                For intento As Integer = 1 To 3
                    Try
                        For Each arch In Directory.GetFiles(carpeta, "*.*")
                            Try
                                File.SetAttributes(arch, FileAttributes.Normal)
                                File.Delete(arch)
                            Catch
                            End Try
                        Next
                        Directory.Delete(carpeta, True)
                        Exit For
                    Catch exIo As IOException
                        Thread.Sleep(80 * intento)
                    Catch exAccess As UnauthorizedAccessException
                        Thread.Sleep(80 * intento)
                    End Try
                Next
            End If
        Catch ex As Exception
            Debug.WriteLine("Aviso: no se pudo limpiar completamente la carpeta " & carpeta & ": " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Comprueba si un PDF ya está procesado y físicamente completo en destino con formato TIFF.
    ''' </summary>
    Public Function EstaPdfYaProcesadoYCompletoTiff(
        origenBase As String,
        destinoBase As String,
        archivoPdf As String,
        ByRef totalPaginas As Integer
    ) As Boolean
        Try
            Dim rutaRelativaArchivo = ObtenerRutaRelativa(origenBase, archivoPdf)
            Dim directorioRelativo = NormalizarRutaRelativa(Path.GetDirectoryName(rutaRelativaArchivo))
            Dim destinoDirectorioFinal = Path.Combine(destinoBase, directorioRelativo)
            Dim nombreSinExt = Path.GetFileNameWithoutExtension(archivoPdf)
            Dim carpetaPdfDestino = Path.Combine(destinoDirectorioFinal, nombreSinExt)

            If Not Directory.Exists(carpetaPdfDestino) Then
                Return False
            End If

            Dim archivosTiff = Directory.GetFiles(carpetaPdfDestino, "*.tiff")
            If archivosTiff.Length = 0 Then
                Return False
            End If

            Dim paginasEsperadas = ObtenerTotalPaginasPdf(archivoPdf)
            If paginasEsperadas <= 0 Then
                Return False
            End If

            totalPaginas = paginasEsperadas

            Dim dummyError As String = ""
            If ValidarIntegridadFisicaTiff(carpetaPdfDestino, nombreSinExt, paginasEsperadas, dummyError) Then
                Return True
            End If

            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Realiza una verificación física rigurosa en disco para garantizar:
    ''' 1. Que la carpeta destino exista.
    ''' 2. Que el conteo físico de archivos TIFF coincida exactamente con las páginas esperadas.
    ''' 3. Que cada archivo consecutivo ({nombreBase}_{i}.tiff) exista físicamente en disco.
    ''' 4. Que ningún archivo esté vacío o incompleto (tamaño >= 16 bytes).
    ''' 5. Que la cabecera mágica corresponda a un archivo TIFF 6.0 estándar ("II*" little-endian o "MM*" big-endian).
    ''' 6. Que la imagen sea completamente legible (validación rápida de metadatos MagickImageInfo sin decodificar píxeles).
    ''' </summary>
    Public Function ValidarIntegridadFisicaTiff(
        carpetaDestino As String,
        nombreBase As String,
        totalPaginasEsperadas As Integer,
        ByRef mensajeError As String
    ) As Boolean
        Try
            If Not Directory.Exists(carpetaDestino) Then
                mensajeError = "La carpeta de destino no existe físicamente en el almacenamiento."
                Return False
            End If

            If totalPaginasEsperadas <= 0 Then
                mensajeError = "El número de páginas esperadas es 0 o inválido."
                Return False
            End If

            ' 1. Obtener todos los archivos TIFF en la carpeta destino
            Dim archivosTiff = Directory.GetFiles(carpetaDestino, "*.tiff")

            ' 2. Validación estricta de cantidad: no pueden haber menos páginas ni archivos sobrantes
            If archivosTiff.Length <> totalPaginasEsperadas Then
                mensajeError = $"Discrepancia en páginas físicas: se esperaban {totalPaginasEsperadas} páginas pero hay {archivosTiff.Length} archivos TIFF en disco."
                Return False
            End If

            ' 3. Validación archivo por archivo de cada página esperada
            For i As Integer = 1 To totalPaginasEsperadas
                Dim archivoEsperado = Path.Combine(carpetaDestino, $"{nombreBase}_{i}.tiff")

                If Not File.Exists(archivoEsperado) Then
                    mensajeError = $"Falta la página física requerida: {nombreBase}_{i}.tiff"
                    Return False
                End If

                Dim fi As New FileInfo(archivoEsperado)
                If fi.Length < 16 Then
                    mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} está dañado o vacío (tamaño: {fi.Length} bytes)."
                    Return False
                End If

                ' 4. Verificación ultrarrápida de Magic Bytes TIFF (TIFF 6.0: II* o MM*)
                Using fs As New FileStream(archivoEsperado, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Dim header(3) As Byte
                    Dim leidos = fs.Read(header, 0, 4)
                    If leidos < 4 Then
                        mensajeError = $"No se pudo leer la cabecera del archivo {Path.GetFileName(archivoEsperado)}."
                        Return False
                    End If

                    Dim esLittleEndian = (header(0) = &H49 AndAlso header(1) = &H49 AndAlso header(2) = &H2A AndAlso header(3) = &H0)
                    Dim esBigEndian = (header(0) = &H4D AndAlso header(1) = &H4D AndAlso header(2) = &H0 AndAlso header(3) = &H2A)

                    If Not esLittleEndian AndAlso Not esBigEndian Then
                        mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} no contiene una cabecera TIFF válida (archivo corrupto)."
                        Return False
                    End If
                End Using

                ' 5. Validación rápida de estructura de imagen con ImageMagick (Ping de metadatos sin descomprimir píxeles)
                Try
                    Dim info As New MagickImageInfo(archivoEsperado)
                    If info.Width <= 0 OrElse info.Height <= 0 OrElse info.Format <> MagickFormat.Tiff Then
                        mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} tiene dimensiones o formato TIFF inválidos ({info.Width}x{info.Height})."
                        Return False
                    End If
                Catch exMagick As Exception
                    mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} está dañado e ilegible: {exMagick.Message}"
                    Return False
                End Try
            Next

            mensajeError = ""
            Return True

        Catch ex As Exception
            mensajeError = "Excepción durante la verificación física de archivos: " & ex.Message
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Obtiene rápidamente el número total de páginas de un PDF sin descomprimirlo en RAM.
    ''' Utiliza iText 7 como método principal con fallback a ImageMagick Ping.
    ''' </summary>
    Public Function ObtenerTotalPaginasPdf(rutaPdf As String) As Integer
        Try
            Using reader As New PdfReader(rutaPdf)
                Using doc As New PdfDocument(reader)
                    Return doc.GetNumberOfPages()
                End Using
            End Using
        Catch
            Try
                Using col As New MagickImageCollection()
                    col.Ping(rutaPdf)
                    Return col.Count
                End Using
            Catch
                Return 0
            End Try
        End Try
    End Function

    ''' <summary>
    ''' Comprueba si un archivo temporal está listo y cerrado para lectura segura.
    ''' </summary>
    Public Function EsArchivoListoParaLectura(rutaArchivo As String) As Boolean
        Try
            If Not File.Exists(rutaArchivo) Then Return False
            Dim fi As New FileInfo(rutaArchivo)
            If fi.Length < 32 Then Return False
            Using fs As New FileStream(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.None)
                Return fs.Length >= 32
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Motor de conversión de ultra-alta velocidad basado en MuPDF CLI (mutool.exe):
    ''' 1. Renderiza con mutool draw a escala de grises a 250 DPI hacia imágenes PNG temporales.
    ''' 2. Codifica en paralelo multihilo a TIFF LZW a 250 DPI.
    ''' 3. Elimina inmediatamente las imágenes temporales para cuidar el disco duro.
    ''' </summary>
    Public Sub ConvertirPdfTiffMuPdf(
        pdfPath As String,
        carpetaDestino As String,
        nombreBase As String,
        ByRef totalPaginas As Integer,
        token As CancellationToken,
        Optional procesarPaginasEnParalelo As Boolean = False,
        Optional hilosParalelos As Integer = 1,
        Optional progresoCallback As Action(Of Integer, String) = Nothing,
        Optional dictProcesos As ConcurrentDictionary(Of Integer, Process) = Nothing
    )
        If token.IsCancellationRequested Then Throw New OperationCanceledException()

        Dim muExe = ModuloRecursos.ObtenerRutaEjecutableMuPdf()
        If String.IsNullOrEmpty(muExe) OrElse Not File.Exists(muExe) Then
            Throw New FileNotFoundException("No se encontró el ejecutable de MuPDF (mutool.exe) en el sistema.")
        End If

        If Not File.Exists(pdfPath) Then
            Throw New FileNotFoundException("No se encontró el archivo PDF: " & pdfPath)
        End If

        If Not Directory.Exists(carpetaDestino) Then
            Directory.CreateDirectory(carpetaDestino)
        End If

        Dim rutaStagingBase = ModuloRecursos.ObtenerRutaStaging()
        Dim stagingCarpeta = Path.Combine(rutaStagingBase, "mu_" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(stagingCarpeta)

        Dim paginasEsperadas = ObtenerTotalPaginasPdf(pdfPath)
        Dim pesoMB = (New FileInfo(pdfPath).Length / (1024.0 * 1024.0))
        Dim procId As Integer = 0

        Try
            If token.IsCancellationRequested Then Throw New OperationCanceledException()

            Dim patronSalidaPng = Path.Combine(stagingCarpeta, "p_%d.png")

            Dim args As New StringBuilder()
            args.Append("draw ")
            args.Append("-q ")
            args.Append("-P ")
            args.Append("-r 250 ")
            args.Append("-c gray ")
            args.Append($"-o ""{patronSalidaPng}"" ")
            args.Append($"""{pdfPath}""")

            Dim psi As New ProcessStartInfo With {
                .FileName = muExe,
                .Arguments = args.ToString(),
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardError = True,
                .RedirectStandardOutput = False
            }

            Dim paginasCodificadas As Integer = 0
            Dim paginasEncontradas As New HashSet(Of Integer)()
            Dim lockerPaginas As New Object()

            Dim codificarPngATiff = Sub(pngFile As String)
                If token.IsCancellationRequested Then Exit Sub

                Dim nomSinExt = Path.GetFileNameWithoutExtension(pngFile)
                Dim partes = nomSinExt.Split("_"c)
                If partes.Length >= 2 Then
                    Dim numPag As Integer = 0
                    If Integer.TryParse(partes(1), numPag) Then
                        SyncLock lockerPaginas
                            If paginasEncontradas.Contains(numPag) Then Exit Sub
                            paginasEncontradas.Add(numPag)
                        End SyncLock

                        Dim tiffDestino = Path.Combine(carpetaDestino, $"{nombreBase}_{numPag}.tiff")

                        Using img As New MagickImage(pngFile)
                            img.Format = MagickFormat.Tiff
                            img.Settings.Compression = CompressionMethod.LZW
                            img.ColorSpace = ColorSpace.Gray
                            img.Density = New Density(250, 250)
                            img.Alpha(AlphaOption.Remove)
                            img.Write(tiffDestino)
                        End Using

                        Try
                            File.Delete(pngFile)
                        Catch
                        End Try

                        Dim compl = Interlocked.Increment(paginasCodificadas)
                        If progresoCallback IsNot Nothing AndAlso (compl Mod 10 = 0 OrElse compl = paginasEsperadas) Then
                            Dim pct = If(paginasEsperadas > 0, CInt(Math.Min(100, (compl / CDbl(paginasEsperadas)) * 100)), 50)
                            Dim totalStr = If(paginasEsperadas > 0, paginasEsperadas.ToString("#,##0"), "?")
                            progresoCallback(pct, $"STATUS|{nombreBase} ({pesoMB:0.1} MB): {compl:#,##0}/{totalStr} págs [MuPDF Pipeline]")
                        End If
                    End If
                End If
            End Sub

            Using proc As New Process()
                proc.StartInfo = psi
                Dim stderrOutput As New StringBuilder()

                AddHandler proc.ErrorDataReceived, Sub(s, e)
                    If Not String.IsNullOrEmpty(e.Data) Then
                        stderrOutput.AppendLine(e.Data)
                    End If
                End Sub

                proc.Start()
                procId = proc.Id
                If dictProcesos IsNot Nothing Then dictProcesos.TryAdd(procId, proc)
                proc.BeginErrorReadLine()

                Using reg = token.Register(Sub()
                    Try
                        If Not proc.HasExited Then proc.Kill()
                    Catch
                    End Try
                End Sub)

                    While Not proc.WaitForExit(75)
                        If token.IsCancellationRequested Then
                            Try
                                If Not proc.HasExited Then proc.Kill()
                            Catch
                            End Try
                            Throw New OperationCanceledException()
                        End If

                        Try
                            Dim archivosPng = Directory.GetFiles(stagingCarpeta, "p_*.png")
                            If archivosPng.Length > 0 Then
                                Dim listos = archivosPng.Where(AddressOf EsArchivoListoParaLectura).ToList()
                                If listos.Count > 0 Then
                                    Dim numHilosEncoding = Math.Max(2, Math.Min(Environment.ProcessorCount, hilosParalelos))
                                    Dim popt As New ParallelOptions With {.MaxDegreeOfParallelism = numHilosEncoding, .CancellationToken = token}
                                    Parallel.ForEach(listos, popt, codificarPngATiff)
                                End If
                            End If
                        Catch
                        End Try
                    End While
                End Using

                If token.IsCancellationRequested Then
                    Throw New OperationCanceledException()
                End If

                If proc.ExitCode <> 0 Then
                    Dim errStr = stderrOutput.ToString().Trim()
                    Throw New IOException($"MuPDF finalizó con código de salida {proc.ExitCode}: {errStr}")
                End If
            End Using

            ' Barrido final para procesar los últimos PNGs que mutool generó justo antes de terminar
            Dim ultimosPng = Directory.GetFiles(stagingCarpeta, "p_*.png")
            If ultimosPng.Length > 0 Then
                Dim numHilosEncoding = Math.Max(2, Math.Min(Environment.ProcessorCount, hilosParalelos))
                Dim popt As New ParallelOptions With {.MaxDegreeOfParallelism = numHilosEncoding, .CancellationToken = token}
                Parallel.ForEach(ultimosPng, popt, codificarPngATiff)
            End If

            Dim tiffGenerados = Directory.GetFiles(carpetaDestino, $"{nombreBase}_*.tiff")
            If tiffGenerados.Length = 0 Then
                Throw New IOException("MuPDF completó la ejecución pero no se generaron archivos TIFF válidos en destino.")
            End If

            totalPaginas = tiffGenerados.Length

        Catch ex As OperationCanceledException
            LimpiarCarpetaIncompleta(carpetaDestino)
            Throw
        Catch ex As Exception
            LimpiarCarpetaIncompleta(carpetaDestino)
            Throw
        Finally
            Dim dummyProc As Process = Nothing
            Try
                If procId > 0 AndAlso dictProcesos IsNot Nothing Then dictProcesos.TryRemove(procId, dummyProc)
            Catch
            End Try
            Try
                If Directory.Exists(stagingCarpeta) Then
                    Directory.Delete(stagingCarpeta, True)
                End If
            Catch
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Carga masivamente todos los registros de la bitácora TIFF en un diccionario en memoria (O(1)).
    ''' Evita hacer 100,000 conexiones individuales a la base de datos, acelerando el análisis de horas a segundos.
    ''' </summary>
    Public Function CargarDiccionarioBitacoraEnMemoria() As Dictionary(Of String, InfoRegistroBd)
        Dim dict As New Dictionary(Of String, InfoRegistroBd)(StringComparer.OrdinalIgnoreCase)
        Try
            Using conn As New MySqlConnection(CadenaConexionMysql)
                conn.Open()
                Dim tabla = NombreTablaBitacora

                Dim sql = $"SELECT documento, nombre_original, estatus, paginas, error FROM {tabla}"
                Using cmd As New MySqlCommand(sql, conn)
                    cmd.CommandTimeout = 120
                    Using dr = cmd.ExecuteReader()
                        While dr.Read()
                            Dim doc = If(IsDBNull(dr("documento")), "", dr("documento").ToString().Trim())
                            Dim orig = If(IsDBNull(dr("nombre_original")), "", dr("nombre_original").ToString().Trim())
                            Dim info As New InfoRegistroBd With {
                                .Encontrado = True,
                                .Estatus = If(IsDBNull(dr("estatus")), "", dr("estatus").ToString().Trim()),
                                .Paginas = If(IsDBNull(dr("paginas")), 0, Convert.ToInt32(dr("paginas"))),
                                .ErrorMsg = If(IsDBNull(dr("error")), "", dr("error").ToString().Trim())
                            }

                            If Not String.IsNullOrEmpty(doc) Then
                                dict(doc) = info
                                dict(Path.GetFileNameWithoutExtension(doc)) = info
                            End If
                            If Not String.IsNullOrEmpty(orig) Then
                                dict(orig) = info
                                dict(Path.GetFileNameWithoutExtension(orig)) = info
                            End If
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Debug.WriteLine("Error al cargar diccionario masivo de BD (TIFF): " & ex.Message)
        End Try
        Return dict
    End Function

    ''' <summary>
    ''' Registra o actualiza en la base de datos MySQL el archivo procesado/reparado con estatus OK en bitacora_transferencia_tiff.
    ''' </summary>
    Public Sub RegistrarExitoEnBd(
        disk As String,
        carpetaOrigen As String,
        carpetaDestino As String,
        documento As String,
        nombreOriginal As String,
        nombreNuevo As String,
        paginas As Integer,
        delegacion As String,
        usuario As String,
        anio As String
    )
        Try
            Using conn As New MySqlConnection(CadenaConexionMysql)
                conn.Open()
                Dim tabla = NombreTablaBitacora

                ' Primero intentar actualizar si ya existía para mantener la base de datos limpia y cuadrada
                Dim sqlUpdate = $"UPDATE {tabla} SET " &
                                "estatus = 'OK', paginas = @pag, error = '', usuario = @usr, fecha = NOW(), " &
                                "carpeta_destino = @dest WHERE documento = @doc OR nombre_original = @orig"
                Dim filasActualizadas As Integer = 0
                Using cmdUpdate As New MySqlCommand(sqlUpdate, conn)
                    cmdUpdate.Parameters.AddWithValue("@pag", paginas)
                    cmdUpdate.Parameters.AddWithValue("@usr", usuario)
                    cmdUpdate.Parameters.AddWithValue("@dest", carpetaDestino)
                    cmdUpdate.Parameters.AddWithValue("@doc", documento)
                    cmdUpdate.Parameters.AddWithValue("@orig", nombreOriginal)
                    filasActualizadas = cmdUpdate.ExecuteNonQuery()
                End Using

                If filasActualizadas = 0 Then
                    Dim sqlInsert = $"INSERT INTO {tabla} " &
                                    "(disk, carpeta_origen, carpeta_destino, documento, nombre_original, nombre_nuevo, error, estatus, paginas, delegacion, usuario, anio, fecha) " &
                                    "VALUES (@disk, @origen, @destino, @doc, @orig, @nuevo, '', 'OK', @pag, @deleg, @usr, @anio, NOW())"
                    Using cmdInsert As New MySqlCommand(sqlInsert, conn)
                        cmdInsert.Parameters.AddWithValue("@disk", disk)
                        cmdInsert.Parameters.AddWithValue("@origen", carpetaOrigen)
                        cmdInsert.Parameters.AddWithValue("@destino", carpetaDestino)
                        cmdInsert.Parameters.AddWithValue("@doc", documento)
                        cmdInsert.Parameters.AddWithValue("@orig", nombreOriginal)
                        cmdInsert.Parameters.AddWithValue("@nuevo", nombreNuevo)
                        cmdInsert.Parameters.AddWithValue("@pag", paginas)
                        cmdInsert.Parameters.AddWithValue("@deleg", delegacion)
                        cmdInsert.Parameters.AddWithValue("@usr", usuario)
                        cmdInsert.Parameters.AddWithValue("@anio", If(String.IsNullOrEmpty(anio), DBNull.Value, anio))
                        cmdInsert.ExecuteNonQuery()
                    End Using
                End If
            End Using
        Catch ex As Exception
            Debug.WriteLine("Error al registrar éxito en BD (TIFF): " & ex.Message)
        End Try
    End Sub

End Module
