Imports System.Collections.Concurrent
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Threading.Tasks
Imports ImageMagick
Imports iText.Kernel.Pdf
Imports MySqlConnector

''' <summary>
''' Módulo centralizado para la conversión de documentos PDF directamente a formato JPG a 200 DPI,
''' verificación física de integridad estricta, y resolución de rutas de entregables.
''' </summary>
Public Module ModuloConversionJpg

    Public Const RESOLUCION_DPI As Integer = 200
    Public Const CALIDAD_JPG As Integer = 85

    ''' <summary>
    ''' Obtiene rápidamente el número total de páginas de un PDF sin descomprimirlo en RAM.
    ''' Implementa múltiples motores de alta resiliencia:
    ''' 1. iText 7 con modo permisivo (UnethicalReading = True).
    ''' 2. MuPDF CLI (mutool show -r 'archivo.pdf' pages) capaz de auto-reparar en memoria.
    ''' 3. Conteo binario por expresiones regulares (/Type /Page o /Count).
    ''' 4. ImageMagick Ping.
    ''' </summary>
    Public Function ObtenerTotalPaginasPdf(rutaPdf As String) As Integer
        If String.IsNullOrWhiteSpace(rutaPdf) OrElse Not File.Exists(rutaPdf) Then Return 0

        ' 1. Intento con iText 7 (permisivo ante restricciones de scanner/permisos)
        Try
            Dim readerProps As New ReaderProperties()
            Using reader As New PdfReader(rutaPdf, readerProps)
                reader.SetUnethicalReading(True)
                Using doc As New PdfDocument(reader)
                    Dim pags = doc.GetNumberOfPages()
                    If pags > 0 Then Return pags
                End Using
            End Using
        Catch exIText As Exception
            ' Continuar con siguiente motor si iText falla
        End Try

        ' 2. Intento con MuPDF CLI (mutool show -r file.pdf pages)
        Try
            Dim pagsMu = ObtenerPaginasPdfConMuPdf(rutaPdf)
            If pagsMu > 0 Then Return pagsMu
        Catch exMu As Exception
        End Try

        ' 3. Intento binario rápido analizando el catálogo /Pages o /Type /Page
        Try
            Dim pagsBin = ObtenerPaginasPdfPorRegex(rutaPdf)
            If pagsBin > 0 Then Return pagsBin
        Catch exBin As Exception
        End Try

        ' 4. Intento con ImageMagick (Ping de metadatos)
        Try
            Using col As New MagickImageCollection()
                col.Ping(rutaPdf)
                If col.Count > 0 Then Return col.Count
            End Using
        Catch
        End Try

        Return 0
    End Function

    ''' <summary>
    ''' Cuenta páginas usando mutool.exe con el comando 'show -r "archivo.pdf" pages'.
    ''' Es extraordinariamente rápido (~2-5ms) y capaz de reparar tablas XREF corruptas en memoria.
    ''' </summary>
    Private Function ObtenerPaginasPdfConMuPdf(rutaPdf As String) As Integer
        Try
            Dim muExe = ModuloRecursos.ObtenerRutaEjecutableMuPdf()
            If String.IsNullOrEmpty(muExe) OrElse Not File.Exists(muExe) Then Return 0

            Dim psi As New ProcessStartInfo With {
                .FileName = muExe,
                .Arguments = $"show -r ""{rutaPdf}"" pages",
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .StandardOutputEncoding = Encoding.UTF8
            }

            Using proc As New Process With {.StartInfo = psi}
                proc.Start()
                Dim salida = proc.StandardOutput.ReadToEnd()
                Dim errSalida = proc.StandardError.ReadToEnd()
                proc.WaitForExit(4000)

                If Not proc.HasExited Then
                    Try
                        proc.Kill()
                    Catch
                    End Try
                    Return 0
                End If

                ' Buscar patrones "page 1 =", "page 2 =", etc.
                Dim matches = Regex.Matches(salida, "(?im)^\s*page\s+(\d+)\s*=")
                If matches.Count > 0 Then
                    Dim maxPag As Integer = 0
                    For Each m As Match In matches
                        Dim num As Integer
                        If Integer.TryParse(m.Groups(1).Value, num) AndAlso num > maxPag Then
                            maxPag = num
                        End If
                    Next
                    Return If(maxPag > 0, maxPag, matches.Count)
                End If
            End Using
        Catch
        End Try
        Return 0
    End Function

    ''' <summary>
    ''' Inspecciona el archivo binario del PDF en busca del catálogo /Pages con /Count
    ''' o del conteo de ocurrencias de /Type /Page.
    ''' </summary>
    Private Function ObtenerPaginasPdfPorRegex(rutaPdf As String) As Integer
        Try
            Dim fi As New FileInfo(rutaPdf)
            If fi.Length = 0 Then Return 0

            ' Leer los primeros 64 KB en busca de /Type /Pages ... /Count N
            Dim tamHeader = Math.Min(CInt(fi.Length), 65536)
            Dim buffer(tamHeader - 1) As Byte

            Using fs As New FileStream(rutaPdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                fs.Read(buffer, 0, tamHeader)
            End Using

            Dim textoInicio = Encoding.ASCII.GetString(buffer)
            Dim mCount = Regex.Match(textoInicio, "/Type\s*/Pages\b[^>]*?/Count\s+(\d+)")
            If mCount.Success Then
                Dim c As Integer
                If Integer.TryParse(mCount.Groups(1).Value, c) AndAlso c > 0 Then Return c
            End If

            ' Si el archivo es menor a 20 MB, escanear todo el archivo en busca de /Type /Page
            If fi.Length <= 20 * 1024 * 1024 Then
                Dim bytesCompletos = File.ReadAllBytes(rutaPdf)
                Dim textoCompleto = Encoding.ASCII.GetString(bytesCompletos)
                Dim matches = Regex.Matches(textoCompleto, "/Type\s*/Page\b")
                If matches.Count > 0 Then Return matches.Count
            End If
        Catch
        End Try
        Return 0
    End Function

    ''' <summary>
    ''' Realiza una verificación física rápida y exhaustiva en disco para archivos JPG:
    ''' 1. Que la carpeta destino exista.
    ''' 2. Que el conteo físico de archivos JPG coincida exactamente con las páginas esperadas.
    ''' 3. Que cada archivo consecutivo ({nombreBase}_{i}.jpg) exista físicamente en disco.
    ''' 4. Que ningún archivo esté vacío (tamaño > 100 bytes).
    ''' 5. Que la cabecera mágica corresponda a JPEG (FF D8 FF).
    ''' 6. Que la imagen sea completamente legible (validación rápida con MagickImageInfo).
    ''' </summary>
    Public Function ValidarIntegridadFisicaJpg(
        carpetaDestino As String,
        nombreBase As String,
        totalPaginasEsperadas As Integer,
        ByRef mensajeError As String
    ) As Boolean
        Try
            If Not Directory.Exists(carpetaDestino) Then
                mensajeError = "La carpeta de destino no existe físicamente."
                Return False
            End If

            If totalPaginasEsperadas <= 0 Then
                mensajeError = "El número de páginas esperadas es 0 o inválido."
                Return False
            End If

            ' 1. Obtener todos los archivos JPG en la carpeta destino
            Dim archivosJpg = Directory.GetFiles(carpetaDestino, "*.jpg")

            ' 2. Validación estricta de cantidad
            If archivosJpg.Length <> totalPaginasEsperadas Then
                mensajeError = $"Discrepancia en páginas físicas: esperadas {totalPaginasEsperadas}, encontradas {archivosJpg.Length} imágenes JPG."
                Return False
            End If

            ' 3. Validación archivo por archivo de cada página esperada
            For i As Integer = 1 To totalPaginasEsperadas
                Dim archivoEsperado = Path.Combine(carpetaDestino, $"{nombreBase}_{i}.jpg")

                If Not File.Exists(archivoEsperado) Then
                    mensajeError = $"Falta la página física requerida: {nombreBase}_{i}.jpg"
                    Return False
                End If

                Dim fi As New FileInfo(archivoEsperado)
                If fi.Length < 100 Then
                    mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} está incompleto o en 0 bytes (tamaño: {fi.Length} bytes)."
                    Return False
                End If

                ' 4. Verificación de Magic Bytes JPG (FF D8 FF)
                Using fs As New FileStream(archivoEsperado, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Dim header(2) As Byte
                    Dim leidos = fs.Read(header, 0, 3)
                    If leidos < 3 OrElse header(0) <> &HFF OrElse header(1) <> &HD8 OrElse header(2) <> &HFF Then
                        mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} no contiene una cabecera JPEG válida."
                        Return False
                    End If
                End Using

                ' 5. Validación rápida de estructura con ImageMagick (Ping de metadatos)
                Try
                    Dim info As New MagickImageInfo(archivoEsperado)
                    If info.Width <= 0 OrElse info.Height <= 0 Then
                        mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} tiene dimensiones inválidas."
                        Return False
                    End If
                Catch exImg As Exception
                    mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} está dañado: {exImg.Message}"
                    Return False
                End Try
            Next

            mensajeError = ""
            Return True

        Catch ex As Exception
            mensajeError = "Error durante la validación física: " & ex.Message
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Validación ultrarrápida para diagnóstico en red: comprueba conteo exacto de archivos y verifica
    ''' las cabeceras de la primera y última página en ~1ms, evitando cientos de llamadas SMB por documento.
    ''' </summary>
    Public Function ValidarIntegridadDiagnosticoRapidoJpg(
        carpetaDestino As String,
        nombreBase As String,
        totalPaginasEsperadas As Integer,
        ByRef mensajeError As String
    ) As Boolean
        Try
            If Not Directory.Exists(carpetaDestino) Then
                mensajeError = "La carpeta de destino no existe físicamente."
                Return False
            End If

            If totalPaginasEsperadas <= 0 Then
                mensajeError = "El número de páginas esperadas es 0 o inválido."
                Return False
            End If

            Dim archivosJpg = Directory.GetFiles(carpetaDestino, "*.jpg")
            If archivosJpg.Length <> totalPaginasEsperadas Then
                mensajeError = $"Discrepancia en páginas físicas: esperadas {totalPaginasEsperadas}, encontradas {archivosJpg.Length} imágenes JPG."
                Return False
            End If

            ' Verificar página inicial (página 1)
            Dim pagInicial = Path.Combine(carpetaDestino, $"{nombreBase}_1.jpg")
            If Not File.Exists(pagInicial) Then
                mensajeError = $"Falta la página inicial física: {nombreBase}_1.jpg"
                Return False
            End If

            Dim fiIni As New FileInfo(pagInicial)
            If fiIni.Length < 100 Then
                mensajeError = $"La página inicial está vacía o incompleta ({fiIni.Length} bytes)."
                Return False
            End If

            Using fs As New FileStream(pagInicial, FileMode.Open, FileAccess.Read, FileShare.Read)
                Dim h(2) As Byte
                If fs.Read(h, 0, 3) < 3 OrElse h(0) <> &HFF OrElse h(1) <> &HD8 OrElse h(2) <> &HFF Then
                    mensajeError = "La página inicial no contiene una cabecera JPEG válida."
                    Return False
                End If
            End Using

            ' Si tiene más de una página, verificar la página final para confirmar que no se cortó la escritura
            If totalPaginasEsperadas > 1 Then
                Dim pagFinal = Path.Combine(carpetaDestino, $"{nombreBase}_{totalPaginasEsperadas}.jpg")
                If Not File.Exists(pagFinal) Then
                    mensajeError = $"Falta la página final física: {nombreBase}_{totalPaginasEsperadas}.jpg"
                    Return False
                End If

                Dim fiFin As New FileInfo(pagFinal)
                If fiFin.Length < 100 Then
                    mensajeError = $"La página final está vacía o incompleta ({fiFin.Length} bytes)."
                    Return False
                End If

                Using fs As New FileStream(pagFinal, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Dim h(2) As Byte
                    If fs.Read(h, 0, 3) < 3 OrElse h(0) <> &HFF OrElse h(1) <> &HD8 OrElse h(2) <> &HFF Then
                        mensajeError = "La página final no contiene una cabecera JPEG válida."
                        Return False
                    End If
                End Using
            End If

            mensajeError = ""
            Return True
        Catch ex As Exception
            mensajeError = "Error en validación rápida de diagnóstico: " & ex.Message
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Comprueba rápidamente si un PDF ya fue convertido completamente en formato JPG.
    ''' Realiza un conteo directo en destino y utiliza el mapa en memoria de la BD para máxima velocidad.
    ''' </summary>
    Public Function EstaPdfYaProcesadoYCompletoJpg(
        origenBase As String,
        destinoBase As String,
        archivoPdf As String,
        ByRef totalPaginas As Integer,
        Optional mapaBd As Dictionary(Of String, InfoRegistroBd) = Nothing
    ) As Boolean
        Try
            Dim rutaRelativaArchivo = ObtenerRutaRelativa(origenBase, archivoPdf)
            Dim directorioRelativo = NormalizarRutaRelativa(Path.GetDirectoryName(rutaRelativaArchivo))
            Dim destinoDirectorioFinal = Path.Combine(destinoBase, directorioRelativo)
            Dim nombreSinExt = Path.GetFileNameWithoutExtension(archivoPdf)
            Dim carpetaPdfDestino = Path.Combine(destinoDirectorioFinal, nombreSinExt)

            ' 1. Si la carpeta no existe, descartar al instante (0 ms)
            If Not Directory.Exists(carpetaPdfDestino) Then Return False

            ' 2. Conteo directo de archivos JPG en la carpeta destino
            Dim archivosJpg = Directory.GetFiles(carpetaPdfDestino, "*.jpg")
            Dim conteoDestino = archivosJpg.Length
            If conteoDestino = 0 Then Return False

            Dim paginasEsperadas As Integer = 0

            ' 3. Consultar la BD en RAM: si ya reporta estatus 'OK' y coincide con el conteo físico, validar cabeceras al vuelo
            If mapaBd IsNot Nothing Then
                Dim infoBd As InfoRegistroBd = Nothing
                If mapaBd.TryGetValue(nombreSinExt, infoBd) OrElse mapaBd.TryGetValue(Path.GetFileName(archivoPdf), infoBd) Then
                    If infoBd.Encontrado AndAlso String.Equals(infoBd.Estatus, "OK", StringComparison.OrdinalIgnoreCase) AndAlso infoBd.Paginas = conteoDestino Then
                        paginasEsperadas = infoBd.Paginas
                    End If
                End If
            End If

            ' 4. Si no coincidió con la BD o no estaba en ella, consultar páginas del PDF
            If paginasEsperadas <= 0 Then
                paginasEsperadas = ObtenerTotalPaginasPdf(archivoPdf)
            End If

            ' 5. Si no se pudieron determinar páginas o el conteo físico en destino no coincide exactamente con el PDF
            If paginasEsperadas <= 0 OrElse conteoDestino <> paginasEsperadas Then
                Return False
            End If

            totalPaginas = paginasEsperadas

            ' 6. Verificación rápida de cabeceras de página inicial y final (1 ms) para garantizar integridad
            Dim dummyError As String = ""
            Return ValidarIntegridadDiagnosticoRapidoJpg(carpetaPdfDestino, nombreSinExt, paginasEsperadas, dummyError)
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Motor de conversión de alta velocidad a JPG a 200 DPI usando MuPDF (mutool.exe) y Magick.NET.
    ''' </summary>
    Public Sub ConvertirPdfJpgMuPdf(
        pdfPath As String,
        carpetaDestino As String,
        nombreBase As String,
        ByRef totalPaginas As Integer,
        token As CancellationToken,
        Optional procesarPaginasEnParalelo As Boolean = False,
        Optional hilosParalelos As Integer = 1,
        Optional onProgreso As Action(Of Integer, String) = Nothing,
        Optional procesactivosMap As ConcurrentDictionary(Of Integer, Process) = Nothing
    )
        If token.IsCancellationRequested Then Throw New OperationCanceledException()

        Dim muExe = ModuloRecursos.ObtenerRutaEjecutableMuPdf()
        If String.IsNullOrEmpty(muExe) OrElse Not File.Exists(muExe) Then
            ModuloRecursos.AsegurarMuPdfInstalado()
            muExe = ModuloRecursos.ObtenerRutaEjecutableMuPdf(True)
        End If

        If String.IsNullOrEmpty(muExe) OrElse Not File.Exists(muExe) Then
            Throw New FileNotFoundException("No se encontró el ejecutable de MuPDF (mutool.exe).")
        End If

        If Not File.Exists(pdfPath) Then
            Throw New FileNotFoundException("No se encontró el archivo PDF: " & pdfPath)
        End If

        Dim rutaStagingBase = ModuloRecursos.ObtenerRutaStaging()
        Dim stagingCarpeta = Path.Combine(rutaStagingBase, "mu_jpg_" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(stagingCarpeta)

        Dim paginasEsperadas = ObtenerTotalPaginasPdf(pdfPath)
        Dim procId As Integer = 0

        Try
            If token.IsCancellationRequested Then Throw New OperationCanceledException()

            Dim patronSalidaPng = Path.Combine(stagingCarpeta, "p_%d.png")

            ' MuPDF: renderizado a 200 DPI directamente
            Dim args As New StringBuilder()
            args.Append("draw ")
            args.Append("-q ")
            args.Append("-P ")
            args.Append($"-r {RESOLUCION_DPI} ")
            args.Append("-c rgb ")
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

            Dim codificarPngAJpg = Sub(pngFile As String)
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

                        ' Codificar directamente en el staging LOCAL para máxima velocidad y evitar errores SMB de ImageMagick
                        Dim jpgLocal = Path.Combine(stagingCarpeta, $"{nombreBase}_{numPag}.jpg")

                        Using img As New MagickImage(pngFile)
                            img.Format = MagickFormat.Jpeg
                            img.Quality = CALIDAD_JPG
                            img.Density = New Density(RESOLUCION_DPI, RESOLUCION_DPI)
                            img.Alpha(AlphaOption.Remove)
                            img.Write(jpgLocal)
                        End Using

                        ' Eliminar temporal al vuelo para no saturar disco
                        Try
                            File.Delete(pngFile)
                        Catch
                        End Try

                        Dim compl = Interlocked.Increment(paginasCodificadas)
                        If onProgreso IsNot Nothing Then
                            Dim pct = If(paginasEsperadas > 0, CInt(Math.Min(100, (compl / CDbl(paginasEsperadas)) * 100)), 50)
                            onProgreso(pct, $"{nombreBase}: {compl}/{paginasEsperadas} págs JPG (200 DPI)")
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
                If procesactivosMap IsNot Nothing Then procesactivosMap.TryAdd(procId, proc)
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
                                    Parallel.ForEach(listos, popt, codificarPngAJpg)
                                End If
                            End If
                        Catch
                        End Try
                    End While

                End Using

                If token.IsCancellationRequested Then Throw New OperationCanceledException()

                If proc.ExitCode <> 0 Then
                    Dim errStr = stderrOutput.ToString().Trim()
                    Throw New IOException($"MuPDF finalizó con código {proc.ExitCode}: {errStr}")
                End If
            End Using

            ' Procesar últimos PNGs generados
            Dim ultimosPng = Directory.GetFiles(stagingCarpeta, "p_*.png")
            If ultimosPng.Length > 0 Then
                Dim numHilosEncoding = Math.Max(2, Math.Min(Environment.ProcessorCount, hilosParalelos))
                Dim popt As New ParallelOptions With {.MaxDegreeOfParallelism = numHilosEncoding, .CancellationToken = token}
                Parallel.ForEach(ultimosPng, popt, codificarPngAJpg)
            End If

            ' Recoger todos los JPGs generados localmente en staging
            Dim jpgsLocales = Directory.GetFiles(stagingCarpeta, $"{nombreBase}_*.jpg")
            If jpgsLocales.Length = 0 Then
                Throw New IOException("MuPDF terminó pero no se generaron archivos JPG válidos en la etapa local.")
            End If

            ' Asegurar la carpeta de destino en almacenamiento de forma segura
            If Not Directory.Exists(carpetaDestino) Then
                Directory.CreateDirectory(carpetaDestino)
                Thread.Sleep(50)
            End If

            ' Transferir de forma segura con .NET administrado (inmune a fallos de codificación nativa en SMB)
            For Each jpgLoc In jpgsLocales
                If token.IsCancellationRequested Then Throw New OperationCanceledException()
                Dim nomArchivo = Path.GetFileName(jpgLoc)
                Dim rutaDestinoFinal = Path.Combine(carpetaDestino, nomArchivo)

                Dim copiado As Boolean = False
                Dim ultimoEx As Exception = Nothing
                For intento As Integer = 1 To 4
                    Try
                        If Not Directory.Exists(carpetaDestino) Then
                            Directory.CreateDirectory(carpetaDestino)
                        End If
                        File.Copy(jpgLoc, rutaDestinoFinal, True)
                        copiado = True
                        Exit For
                    Catch exCopy As Exception
                        ultimoEx = exCopy
                        Thread.Sleep(150 * intento)
                        Try
                            Directory.CreateDirectory(carpetaDestino)
                        Catch
                        End Try
                    End Try
                Next

                If Not copiado AndAlso ultimoEx IsNot Nothing Then
                    Throw New IOException($"Error al transferir '{nomArchivo}' a destino: {ultimoEx.Message}", ultimoEx)
                End If
            Next

            ' Validar archivos JPG generados en destino
            Dim jpgGenerados = Directory.GetFiles(carpetaDestino, $"{nombreBase}_*.jpg")
            If jpgGenerados.Length = 0 Then
                Throw New IOException("No se encontraron archivos JPG en la carpeta destino final.")
            End If

            totalPaginas = jpgGenerados.Length

        Catch ex As OperationCanceledException
            VaciarArchivosDeCarpeta(carpetaDestino)
            Throw
        Catch ex As Exception
            VaciarArchivosDeCarpeta(carpetaDestino)
            Throw
        Finally
            Dim dummyProc As Process = Nothing
            If procesactivosMap IsNot Nothing AndAlso procId > 0 Then
                procesactivosMap.TryRemove(procId, dummyProc)
            End If
            Try
                If Directory.Exists(stagingCarpeta) Then
                    Directory.Delete(stagingCarpeta, True)
                End If
            Catch
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Elimina únicamente los archivos JPG dentro de la carpeta destino sin destruir
    ''' el objeto de directorio del sistema de archivos, previniendo colisiones de bloqueo SMB (DELETE_PENDING).
    ''' </summary>
    Public Sub VaciarArchivosDeCarpeta(carpetaDestino As String)
        Try
            If String.IsNullOrWhiteSpace(carpetaDestino) OrElse Not Directory.Exists(carpetaDestino) Then Exit Sub
            Dim archivos = Directory.GetFiles(carpetaDestino, "*.jpg")
            For Each f In archivos
                Try
                    File.Delete(f)
                Catch
                End Try
            Next
        Catch ex As Exception
            Debug.WriteLine("Error al vaciar archivos de carpeta: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Comprueba si un archivo en disco está completamente cerrado y accesible para lectura.
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
    ''' Elimina la carpeta destino y sus archivos si el proceso fue cancelado o quedó incompleto.
    ''' </summary>
    Public Sub LimpiarCarpetaIncompleta(carpetaDestino As String)
        Try
            If String.IsNullOrWhiteSpace(carpetaDestino) OrElse Not Directory.Exists(carpetaDestino) Then Exit Sub
            Dim archivos = Directory.GetFiles(carpetaDestino, "*.*")
            For Each f In archivos
                Try
                    File.Delete(f)
                Catch
                End Try
            Next
            Directory.Delete(carpetaDestino, True)
        Catch ex As Exception
            Debug.WriteLine("Error al limpiar carpeta incompleta: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Obtiene la ruta relativa respecto a la base.
    ''' </summary>
    Public Function ObtenerRutaRelativa(basePath As String, fullPath As String) As String
        Try
            If Not basePath.EndsWith("\") Then
                basePath &= "\"
            End If
            Dim uriBase As New Uri(basePath)
            Dim uriFull As New Uri(fullPath)
            Dim relativeUri As Uri = uriBase.MakeRelativeUri(uriFull)
            Dim relativePath As String = Uri.UnescapeDataString(relativeUri.ToString())
            Return relativePath.Replace("/"c, "\"c)
        Catch ex As Exception
            Return fullPath.Replace(basePath, "").TrimStart("\"c)
        End Try
    End Function

    ''' <summary>
    ''' Normaliza carpetas tipo 'ENTREGA X' en 'ENTREGABLE X'.
    ''' </summary>
    Public Function NormalizarRutaRelativa(directorioRelativo As String) As String
        If String.IsNullOrWhiteSpace(directorioRelativo) Then Return ""
        Dim partes = directorioRelativo.Split(New Char() {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}, StringSplitOptions.None)
        For i As Integer = 0 To partes.Length - 1
            Dim m = System.Text.RegularExpressions.Regex.Match(partes(i).Trim(), "^ENTREGA[\s_-]*(\d+.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            If m.Success Then
                partes(i) = "ENTREGABLE " & m.Groups(1).Value.Trim()
            End If
        Next
        Return Path.Combine(partes)
    End Function

    ''' <summary>
    ''' Resuelve la ruta destino efectiva respetando entregables.
    ''' </summary>
    Public Function ResolverDestinoEfectivo(origen As String, destino As String) As String
        Try
            If String.IsNullOrWhiteSpace(origen) OrElse String.IsNullOrWhiteSpace(destino) Then
                Return destino
            End If

            Dim nombreOrigen = Path.GetFileName(origen.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Trim()
            Dim subCarpetaDestino = nombreOrigen

            Dim mEntrega = System.Text.RegularExpressions.Regex.Match(nombreOrigen, "^ENTREGA[\s_-]*(\d+.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            If mEntrega.Success Then
                subCarpetaDestino = "ENTREGABLE " & mEntrega.Groups(1).Value.Trim()
            End If

            Dim nombreDestino = Path.GetFileName(destino.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Trim()

            If nombreDestino.Equals(subCarpetaDestino, StringComparison.OrdinalIgnoreCase) OrElse
               nombreDestino.Equals(nombreOrigen, StringComparison.OrdinalIgnoreCase) Then
                Return destino
            End If

            Dim esEntregable = mEntrega.Success OrElse System.Text.RegularExpressions.Regex.IsMatch(nombreOrigen, "^ENTREGABLE[\s_-]*(\d+.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            If esEntregable Then
                Return Path.Combine(destino, subCarpetaDestino)
            End If

            Return destino
        Catch ex As Exception
            Return destino
        End Try
    End Function

    ''' <summary>
    ''' Información de un registro consultado en la base de datos de bitácora.
    ''' </summary>
    Public Class InfoRegistroBd
        Public Property Encontrado As Boolean = False
        Public Property Estatus As String = ""
        Public Property Paginas As Integer = 0
        Public Property ErrorMsg As String = ""
        Public Property Fecha As DateTime? = Nothing
    End Class

    ''' <summary>
    ''' Cadena de conexión activa para MySQL.
    ''' </summary>
    Public Property CadenaConexionMysql As String = "server=127.0.0.1;port=3307;user id=appuser;password=1234;database=stellumdb;ConnectionTimeout=3;Pooling=true;MinPoolSize=1;MaxPoolSize=10;"

    ''' <summary>
    ''' Comprueba si el servidor MySQL está accesible.
    ''' </summary>
    Public Function ProbarConexionMysql() As Boolean
        Try
            Using conn As New MySqlConnection(CadenaConexionMysql)
                conn.Open()
                Return True
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Nombre de la tabla principal de bitácora para transferencias a JPG.
    ''' </summary>
    Public Property NombreTablaBitacora As String = "bitacora_transferencia_jpg"

    ''' <summary>
    ''' Consulta en la tabla bitacora_transferencia_jpg el estatus de un documento por nombre o nombre_original.
    ''' </summary>
    Public Function ConsultarRegistroEnBd(nombreDocumento As String) As InfoRegistroBd
        Dim info As New InfoRegistroBd()
        Try
            Dim nombreSinExt = Path.GetFileNameWithoutExtension(nombreDocumento)
            Dim nombreConExt = Path.GetFileName(nombreDocumento)

            Using conn As New MySqlConnection(CadenaConexionMysql)
                conn.Open()
                Dim tabla = NombreTablaBitacora
                Dim queryOk As Boolean = False

                For Each tbl In {tabla, "bitacora_transferencia_tiff"}
                    Try
                        Dim sql = $"SELECT estatus, paginas, error, fecha FROM {tbl} " &
                                  "WHERE (documento = @nom1 OR documento = @nom2 OR nombre_original = @nom1 OR nombre_original = @nom2) " &
                                  "ORDER BY id DESC LIMIT 1"
                        Using cmd As New MySqlCommand(sql, conn)
                            cmd.Parameters.AddWithValue("@nom1", nombreSinExt)
                            cmd.Parameters.AddWithValue("@nom2", nombreConExt)
                            Using dr = cmd.ExecuteReader()
                                If dr.Read() Then
                                    info.Encontrado = True
                                    info.Estatus = If(IsDBNull(dr("estatus")), "", dr("estatus").ToString())
                                    info.Paginas = If(IsDBNull(dr("paginas")), 0, Convert.ToInt32(dr("paginas")))
                                    info.ErrorMsg = If(IsDBNull(dr("error")), "", dr("error").ToString())
                                    If Not IsDBNull(dr("fecha")) Then
                                        info.Fecha = Convert.ToDateTime(dr("fecha"))
                                    End If
                                End If
                            End Using
                        End Using
                        queryOk = True
                        Exit For
                    Catch exTbl As MySqlException When exTbl.Number = 1146
                        ' Si la tabla no existe, probar con la siguiente tabla de compatibilidad
                    End Try
                Next
            End Using
        Catch ex As Exception
            info.ErrorMsg = ex.Message
        End Try
        Return info
    End Function

    ''' <summary>
    ''' Carga masivamente todos los registros de la bitácora en un diccionario en memoria (O(1)).
    ''' Evita hacer 100,000 conexiones individuales a la base de datos, acelerando el análisis de horas a segundos.
    ''' </summary>
    Public Function CargarDiccionarioBitacoraEnMemoria() As Dictionary(Of String, InfoRegistroBd)
        Dim dict As New Dictionary(Of String, InfoRegistroBd)(StringComparer.OrdinalIgnoreCase)
        Try
            Using conn As New MySqlConnection(CadenaConexionMysql)
                conn.Open()
                Dim tabla = NombreTablaBitacora

                For Each tbl In {tabla, "bitacora_transferencia_tiff"}
                    Try
                        Dim sql = $"SELECT documento, nombre_original, estatus, paginas, error FROM {tbl} ORDER BY id ASC"
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
                        Exit For
                    Catch exTbl As MySqlException When exTbl.Number = 1146
                        ' Probar tabla alternativa
                    End Try
                Next
            End Using
        Catch ex As Exception
            Debug.WriteLine("Error al cargar diccionario masivo de BD: " & ex.Message)
        End Try
        Return dict
    End Function

    ''' <summary>
    ''' Registra o actualiza en la base de datos MySQL el archivo procesado/reparado con estatus OK.
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

                For Each tbl In {tabla, "bitacora_transferencia_tiff"}
                    Try
                        ' Primero intentar actualizar si ya existía para mantener la base de datos limpia y cuadrada
                        Dim sqlUpdate = $"UPDATE {tbl} SET " &
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
                            Dim sqlInsert = $"INSERT INTO {tbl} " &
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
                                cmdInsert.Parameters.AddWithValue("@anio", If(String.IsNullOrEmpty(anio), DBNull.Value, CObj(anio)))
                                cmdInsert.ExecuteNonQuery()
                            End Using
                        End If
                        Exit For
                    Catch exTbl As MySqlException When exTbl.Number = 1146
                        ' Si la tabla no existe, probar con la alternativa
                    End Try
                Next
            End Using
        Catch ex As Exception
            ' Fallback asíncrono o bitácora local
            ModuloBitacoraAsync.EncolarTransferenciaJpg(
                disk, carpetaOrigen, carpetaDestino, documento, nombreOriginal, nombreNuevo, "", "OK", paginas, delegacion, usuario, anio
            )
        End Try
    End Sub

End Module
