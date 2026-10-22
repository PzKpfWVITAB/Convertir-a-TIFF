Imports System.Collections.Concurrent
Imports System.Diagnostics
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
    ''' Convierte cualquier ruta UNC (\\servidor\recurso\...) o local (C:\...) al formato extendido Win32
    ''' (\\?\UNC\servidor\recurso\... o \\?\C:\...) para superar el límite de 260 caracteres (MAX_PATH)
    ''' hasta 32,767 caracteres sin errores DirectoryNotFoundException ni PathTooLongException.
    ''' </summary>
    Public Function APrefijoRutaLarga(ruta As String) As String
        If String.IsNullOrWhiteSpace(ruta) Then Return ruta
        Dim r = ruta.Trim().Replace("/"c, "\"c)
        If r.StartsWith("\\?\", StringComparison.Ordinal) Then Return r
        If r.StartsWith("\\", StringComparison.Ordinal) Then
            Return "\\?\UNC\" & r.Substring(2)
        End If
        If r.Length >= 2 AndAlso r(1) = ":"c Then
            Return "\\?\" & r
        End If
        Return r
    End Function

    ''' <summary>
    ''' Devuelve la ruta limpia sin el prefijo extendido Win32 (\\?\UNC\ o \\?\) para visualización o base de datos.
    ''' </summary>
    Public Function QuitarPrefijoRutaLarga(ruta As String) As String
        If String.IsNullOrWhiteSpace(ruta) Then Return ruta
        If ruta.StartsWith("\\?\UNC\", StringComparison.OrdinalIgnoreCase) Then
            Return "\\" & ruta.Substring(8)
        End If
        If ruta.StartsWith("\\?\", StringComparison.OrdinalIgnoreCase) Then
            Return ruta.Substring(4)
        End If
        Return ruta
    End Function

    ''' <summary>
    ''' Obtiene rápidamente el número total de páginas de un PDF sin descomprimirlo en RAM.
    ''' Implementa múltiples motores de alta resiliencia:
    ''' 1. Inspección binaria directa de cabecera/trailer (/Count N) en 1 ms.
    ''' 2. MuPDF CLI (mutool show -r 'archivo.pdf' pages) capaz de auto-reparar en memoria.
    ''' 3. iText 7 con modo permisivo (UnethicalReading = True).
    ''' 4. ImageMagick Ping.
    ''' </summary>
    Public Function ObtenerTotalPaginasPdf(rutaPdf As String) As Integer
        Dim rutaLarga = APrefijoRutaLarga(rutaPdf)
        If String.IsNullOrWhiteSpace(rutaPdf) OrElse Not File.Exists(rutaLarga) Then Return 0

        ' 1. INTENTO ULTRARRÁPIDO EN 1 MS: Inspección binaria directa de cabecera/trailer (/Count N)
        Try
            Dim pagsBin = ObtenerPaginasPdfPorRegex(rutaLarga)
            If pagsBin > 0 Then Return pagsBin
        Catch exBin As Exception
        End Try

        ' 2. Intento con MuPDF CLI (mutool show -r file.pdf pages) (~2-5 ms)
        Try
            Dim pagsMu = ObtenerPaginasPdfConMuPdf(rutaPdf)
            If pagsMu > 0 Then Return pagsMu
        Catch exMu As Exception
        End Try

        ' 3. Intento con iText 7 (permisivo ante restricciones de scanner/permisos) (~200-500 ms)
        Try
            Dim readerProps As New ReaderProperties()
            Using fs As New FileStream(rutaLarga, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Using reader As New PdfReader(fs, readerProps)
                    reader.SetUnethicalReading(True)
                    Using doc As New PdfDocument(reader)
                        Dim pags = doc.GetNumberOfPages()
                        If pags > 0 Then Return pags
                    End Using
                End Using
            End Using
        Catch exIText As Exception
        End Try

        ' 4. Intento con ImageMagick (Ping de metadatos)
        Try
            Using fs As New FileStream(rutaLarga, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Using col As New MagickImageCollection()
                    col.Ping(fs)
                    If col.Count > 0 Then Return col.Count
                End Using
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
        Dim archivoTempCorto As String = Nothing
        Try
            Dim muExe = ModuloRecursos.ObtenerRutaEjecutableMuPdf()
            If String.IsNullOrEmpty(muExe) OrElse Not File.Exists(muExe) Then Return 0

            Dim rutaEfectiva = rutaPdf
            If rutaPdf.Length >= 240 Then
                archivoTempCorto = Path.Combine(ModuloRecursos.ObtenerRutaStaging(), $"cnt_{Process.GetCurrentProcess().Id}_{Guid.NewGuid():N}.pdf")
                File.Copy(APrefijoRutaLarga(rutaPdf), archivoTempCorto, True)
                rutaEfectiva = archivoTempCorto
            End If

            Dim psi As New ProcessStartInfo With {
                .FileName = muExe,
                .Arguments = $"show -r ""{rutaEfectiva}"" pages",
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
        Finally
            If Not String.IsNullOrEmpty(archivoTempCorto) Then
                Try
                    File.Delete(archivoTempCorto)
                Catch
                End Try
            End If
        End Try
        Return 0
    End Function

    ''' <summary>
    ''' Inspecciona el archivo binario del PDF en busca del catálogo /Pages con /Count
    ''' en ~1ms por salto SMB.
    ''' </summary>
    Private Function ObtenerPaginasPdfPorRegex(rutaPdf As String) As Integer
        Try
            Dim rutaLarga = APrefijoRutaLarga(rutaPdf)
            Dim fi As New FileInfo(rutaLarga)
            If fi.Length = 0 Then Return 0

            Dim tamLectura = Math.Min(CInt(fi.Length), 131072)
            Dim buffer(tamLectura - 1) As Byte

            Using fs As New FileStream(rutaLarga, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Dim leidos = fs.Read(buffer, 0, tamLectura)
                If leidos > 0 Then
                    Dim textoInicio = Encoding.ASCII.GetString(buffer, 0, leidos)
                    Dim pags = ExtraerCountDeTexto(textoInicio)
                    If pags > 0 Then Return pags
                End If

                If fi.Length > tamLectura Then
                    Dim tamTrailer = Math.Min(CInt(fi.Length), 131072)
                    Dim bufferTrailer(tamTrailer - 1) As Byte
                    fs.Seek(-tamTrailer, SeekOrigin.End)
                    Dim leidosTrailer = fs.Read(bufferTrailer, 0, tamTrailer)
                    If leidosTrailer > 0 Then
                        Dim textoTrailer = Encoding.ASCII.GetString(bufferTrailer, 0, leidosTrailer)
                        Dim pagsTrailer = ExtraerCountDeTexto(textoTrailer)
                        If pagsTrailer > 0 Then Return pagsTrailer
                    End If
                End If
            End Using
        Catch
        End Try
        Return 0
    End Function

    Private Function ExtraerCountDeTexto(texto As String) As Integer
        If String.IsNullOrEmpty(texto) Then Return 0

        Dim m = Regex.Match(texto, "/Type\s*/Pages\b[\s\S]{0,1000}?/Count\s+(\d+)")
        If m.Success Then
            Dim c As Integer
            If Integer.TryParse(m.Groups(1).Value, c) AndAlso c > 0 Then Return c
        End If

        m = Regex.Match(texto, "/Count\s+(\d+)[\s\S]{0,1000}?/Type\s*/Pages\b")
        If m.Success Then
            Dim c As Integer
            If Integer.TryParse(m.Groups(1).Value, c) AndAlso c > 0 Then Return c
        End If

        m = Regex.Match(texto, "/Pages\b[^>]*?/Count\s+(\d+)")
        If m.Success Then
            Dim c As Integer
            If Integer.TryParse(m.Groups(1).Value, c) AndAlso c > 0 Then Return c
        End If

        Return 0
    End Function

    ''' <summary>
    ''' Realiza una verificación física rápida y exhaustiva en disco para archivos JPG:
    ''' 1. Que la carpeta destino exista.
    ''' 2. Que el conteo físico de archivos JPG coincida exactamente con las páginas esperadas.
    ''' 3. Que cada archivo consecutivo ({nombreBase}_{i}.jpg) exista físicamente en disco.
    ''' 4. Que ningún archivo esté vacío (tamaño >= 100 bytes).
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
            Dim carpetaLarga = APrefijoRutaLarga(carpetaDestino)
            If Not Directory.Exists(carpetaLarga) Then
                mensajeError = "La carpeta de destino no existe físicamente."
                Return False
            End If

            If totalPaginasEsperadas <= 0 Then
                mensajeError = "El número de páginas esperadas es 0 o inválido."
                Return False
            End If

            Dim archivosJpg = Directory.GetFiles(carpetaLarga, "*.jpg")

            If archivosJpg.Length <> totalPaginasEsperadas Then
                mensajeError = $"Discrepancia en páginas físicas: esperadas {totalPaginasEsperadas}, encontradas {archivosJpg.Length} imágenes JPG."
                Return False
            End If

            For i As Integer = 1 To totalPaginasEsperadas
                Dim archivoEsperado = APrefijoRutaLarga(Path.Combine(carpetaDestino, $"{nombreBase}_{i}.jpg"))

                If Not File.Exists(archivoEsperado) Then
                    mensajeError = $"Falta la página física requerida: {nombreBase}_{i}.jpg"
                    Return False
                End If

                Dim fi As New FileInfo(archivoEsperado)
                If fi.Length < 100 Then
                    mensajeError = $"El archivo {nombreBase}_{i}.jpg está incompleto o en 0 bytes (tamaño: {fi.Length} bytes)."
                    Return False
                End If

                Using fs As New FileStream(archivoEsperado, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Dim header(2) As Byte
                    Dim leidos = fs.Read(header, 0, 3)
                    If leidos < 3 OrElse header(0) <> &HFF OrElse header(1) <> &HD8 OrElse header(2) <> &HFF Then
                        mensajeError = $"El archivo {nombreBase}_{i}.jpg no contiene una cabecera JPEG válida."
                        Return False
                    End If
                End Using

                Try
                    Using fsInfo As New FileStream(archivoEsperado, FileMode.Open, FileAccess.Read, FileShare.Read)
                        Dim info As New MagickImageInfo(fsInfo)
                        If info.Width <= 0 OrElse info.Height <= 0 Then
                            mensajeError = $"El archivo {nombreBase}_{i}.jpg tiene dimensiones inválidas."
                            Return False
                        End If
                    End Using
                Catch exImg As Exception
                    mensajeError = $"El archivo {nombreBase}_{i}.jpg está dañado: {exImg.Message}"
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
    ''' Validación ultrarrápida para diagnóstico y posconversión en red:
    ''' Usa DirectoryInfo.GetFiles en una sola llamada SMB para comprobar:
    ''' 1. Conteo exacto de archivos.
    ''' 2. Existencia de toda la secuencia {nombreBase}_1.jpg .. {nombreBase}_N.jpg en RAM (0 llamadas extra).
    ''' 3. Que ninguna página intermedia ni extrema tenga 0 bytes (menor a 100 bytes).
    ''' 4. Cabecera mágica JPEG (FF D8 FF) de la primera y última página.
    ''' </summary>
    Public Function ValidarIntegridadDiagnosticoRapidoJpg(
        carpetaDestino As String,
        nombreBase As String,
        totalPaginasEsperadas As Integer,
        ByRef mensajeError As String
    ) As Boolean
        Try
            Dim carpetaLarga = APrefijoRutaLarga(carpetaDestino)
            Dim di As New DirectoryInfo(carpetaLarga)
            If Not di.Exists Then
                mensajeError = "La carpeta de destino no existe físicamente."
                Return False
            End If

            If totalPaginasEsperadas <= 0 Then
                mensajeError = "El número de páginas esperadas es 0 o inválido."
                Return False
            End If

            ' 1 sola llamada SMB que trae nombres y tamaños en bytes de todos los archivos de la carpeta
            Dim archivosInfo = di.GetFiles("*.jpg")
            If archivosInfo.Length <> totalPaginasEsperadas Then
                mensajeError = $"Discrepancia en páginas físicas: esperadas {totalPaginasEsperadas}, encontradas {archivosInfo.Length} imágenes JPG."
                Return False
            End If

            Dim mapaArchivos As New Dictionary(Of String, Long)(archivosInfo.Length, StringComparer.OrdinalIgnoreCase)
            For Each fi In archivosInfo
                If fi.Length < 100 Then
                    mensajeError = $"La página '{fi.Name}' está vacía o truncada ({fi.Length} bytes)."
                    Return False
                End If
                mapaArchivos(fi.Name) = fi.Length
            Next

            ' Verificar que exista la secuencia consecutiva completa 1..N en RAM (0 llamadas SMB)
            For i As Integer = 1 To totalPaginasEsperadas
                Dim nomEsperado = $"{nombreBase}_{i}.jpg"
                If Not mapaArchivos.ContainsKey(nomEsperado) Then
                    mensajeError = $"Falta la página física requerida: {nomEsperado}"
                    Return False
                End If
            Next

            ' Verificar cabecera binaria de página inicial (página 1)
            Dim pagInicial = APrefijoRutaLarga(Path.Combine(carpetaDestino, $"{nombreBase}_1.jpg"))
            Using fs As New FileStream(pagInicial, FileMode.Open, FileAccess.Read, FileShare.Read)
                Dim h(2) As Byte
                If fs.Read(h, 0, 3) < 3 OrElse h(0) <> &HFF OrElse h(1) <> &HD8 OrElse h(2) <> &HFF Then
                    mensajeError = "La página inicial no contiene una cabecera JPEG válida."
                    Return False
                End If
            End Using

            ' Si tiene más de una página, verificar cabecera binaria de la página final
            If totalPaginasEsperadas > 1 Then
                Dim pagFinal = APrefijoRutaLarga(Path.Combine(carpetaDestino, $"{nombreBase}_{totalPaginasEsperadas}.jpg"))
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
    ''' Realiza un conteo directo en destino y utiliza la caché de directorios padre para máxima velocidad sobre SMB.
    ''' </summary>
    Public Function EstaPdfYaProcesadoYCompletoJpg(
        origenBase As String,
        destinoBase As String,
        archivoPdf As String,
        ByRef totalPaginas As Integer,
        Optional cacheDirectoriosDestino As ConcurrentDictionary(Of String, Lazy(Of HashSet(Of String))) = Nothing,
        Optional mapaIndice As Dictionary(Of String, ModuloIndiceOrigen.InfoIndicePdf) = Nothing,
        Optional mapaBd As Dictionary(Of String, InfoRegistroBd) = Nothing
    ) As Boolean
        Try
            Dim rutaRelativaArchivo = ObtenerRutaRelativa(origenBase, archivoPdf)
            Dim directorioRelativo = NormalizarRutaRelativa(Path.GetDirectoryName(rutaRelativaArchivo))
            Dim destinoDirectorioFinal = Path.Combine(destinoBase, directorioRelativo)
            Dim nombreSinExt = Path.GetFileNameWithoutExtension(archivoPdf)
            Dim carpetaPdfDestino = Path.Combine(destinoDirectorioFinal, nombreSinExt)
            Dim carpetaPdfDestinoLarga = APrefijoRutaLarga(carpetaPdfDestino)

            ' 1. Verificación instantánea con caché en RAM del directorio padre (evita miles de llamadas SMB)
            If cacheDirectoriosDestino IsNot Nothing Then
                Dim lazySet = cacheDirectoriosDestino.GetOrAdd(destinoDirectorioFinal, Function(dirPadre)
                    Return New Lazy(Of HashSet(Of String))(Function()
                        Dim hs As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                        Try
                            Dim dirPadreLargo = APrefijoRutaLarga(dirPadre)
                            If Directory.Exists(dirPadreLargo) Then
                                For Each subDir In Directory.GetDirectories(dirPadreLargo)
                                    hs.Add(Path.GetFileName(subDir))
                                Next
                            End If
                        Catch
                        End Try
                        Return hs
                    End Function, LazyThreadSafetyMode.ExecutionAndPublication)
                End Function)

                If Not lazySet.Value.Contains(nombreSinExt) Then
                    Return False
                End If
            Else
                If Not Directory.Exists(carpetaPdfDestinoLarga) Then Return False
            End If

            ' 2. Determinar páginas esperadas desde el índice en memoria o BD (con estatus OK)
            Dim paginasEsperadas As Integer = 0
            If mapaIndice IsNot Nothing Then
                Dim infoIndice As ModuloIndiceOrigen.InfoIndicePdf = Nothing
                If mapaIndice.TryGetValue(archivoPdf, infoIndice) AndAlso infoIndice.Paginas > 0 Then
                    paginasEsperadas = infoIndice.Paginas
                ElseIf mapaIndice.TryGetValue(Path.GetFileName(archivoPdf), infoIndice) AndAlso infoIndice.Paginas > 0 Then
                    paginasEsperadas = infoIndice.Paginas
                End If
            End If

            Dim infoBd As InfoRegistroBd = Nothing
            If mapaBd IsNot Nothing Then
                Dim carpetaDestKey = carpetaPdfDestino.TrimEnd("\"c, "/"c)
                If Not mapaBd.TryGetValue(carpetaDestKey, infoBd) Then
                    If Not mapaBd.TryGetValue(archivoPdf, infoBd) Then
                        If Not mapaBd.TryGetValue(nombreSinExt, infoBd) Then
                            mapaBd.TryGetValue(Path.GetFileName(archivoPdf), infoBd)
                        End If
                    End If
                End If

                If paginasEsperadas <= 0 AndAlso infoBd IsNot Nothing AndAlso infoBd.Encontrado AndAlso
                   String.Equals(infoBd.Estatus, "OK", StringComparison.OrdinalIgnoreCase) AndAlso infoBd.Paginas > 0 Then
                    paginasEsperadas = infoBd.Paginas
                End If
            End If

            ' 3. Si conocemos las páginas esperadas por BD/Índice y el estatus en BD ya es OK:
            If paginasEsperadas > 0 AndAlso infoBd IsNot Nothing AndAlso String.Equals(infoBd.Estatus, "OK", StringComparison.OrdinalIgnoreCase) Then
                Dim archivosJpg = Directory.GetFiles(carpetaPdfDestinoLarga, "*.jpg")
                If archivosJpg.Length = paginasEsperadas Then
                    totalPaginas = paginasEsperadas
                    Return True
                Else
                    Return False
                End If
            End If

            ' 4. Si la carpeta destino existe pero NO tenía registro OK en BD (o el índice tiene Paginas=0):
            ' Es OBLIGATORIO leer las páginas reales del PDF de origen (1ms por regex binario) para jamás asumir un corte parcial como completo
            If paginasEsperadas <= 0 Then
                paginasEsperadas = ObtenerTotalPaginasPdf(archivoPdf)
            End If

            If paginasEsperadas <= 0 Then Return False

            Dim dummyError As String = ""
            If ValidarIntegridadDiagnosticoRapidoJpg(carpetaPdfDestino, nombreSinExt, paginasEsperadas, dummyError) Then
                totalPaginas = paginasEsperadas
                Return True
            End If

            Return False
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

        Dim pdfPathLargo = APrefijoRutaLarga(pdfPath)
        If Not File.Exists(pdfPathLargo) Then
            Throw New FileNotFoundException("No se encontró el archivo PDF: " & pdfPath)
        End If

        Dim rutaStagingBase = ModuloRecursos.ObtenerRutaStaging()
        Dim pidActual = Process.GetCurrentProcess().Id
        Dim stagingCarpeta = Path.Combine(rutaStagingBase, $"mu_jpg_{pidActual}_{Guid.NewGuid():N}")
        Directory.CreateDirectory(stagingCarpeta)

        Dim paginasEsperadas = ObtenerTotalPaginasPdf(pdfPath)
        Dim procId As Integer = 0

        Try
            If token.IsCancellationRequested Then Throw New OperationCanceledException()

            ' Si la ruta del PDF de origen es muy larga (>= 240 caracteres), copiar primero a staging local con ruta corta
            ' para evitar que el binario nativo mutool.exe falle por MAX_PATH (260 caracteres) en fopen()
            Dim pdfInputParaMuPdf As String = pdfPath
            If pdfPath.Length >= 240 Then
                pdfInputParaMuPdf = Path.Combine(stagingCarpeta, "doc_origen.pdf")
                File.Copy(pdfPathLargo, pdfInputParaMuPdf, True)
            End If

            Dim patronSalidaPng = Path.Combine(stagingCarpeta, "p_%d.png")

            ' MuPDF: renderizado a 200 DPI directamente
            Dim args As New StringBuilder()
            args.Append("draw ")
            args.Append("-q ")
            args.Append("-P ")
            args.Append($"-r {RESOLUCION_DPI} ")
            args.Append("-c rgb ")
            args.Append($"-o ""{patronSalidaPng}"" ")
            args.Append($"""{pdfInputParaMuPdf}""")

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

            If paginasEsperadas > 0 AndAlso jpgsLocales.Length <> paginasEsperadas Then
                Throw New IOException($"Discrepancia en staging local: el PDF tiene {paginasEsperadas} páginas pero solo se generaron {jpgsLocales.Length} imágenes JPG.")
            End If

            ' Asegurar la carpeta de destino en almacenamiento de forma segura usando ruta extendida Win32 (\\?\UNC\...)
            Dim carpetaDestinoLarga = APrefijoRutaLarga(carpetaDestino)
            If Not Directory.Exists(carpetaDestinoLarga) Then
                Directory.CreateDirectory(carpetaDestinoLarga)
                Thread.Sleep(50)
            End If

            ' Transferir de forma segura con .NET administrado y soporte de rutas largas (> 260 caracteres)
            Dim numHilosCopia = Math.Max(2, Math.Min(6, Environment.ProcessorCount \ 2))
            Dim poptCopia As New ParallelOptions With {.MaxDegreeOfParallelism = numHilosCopia, .CancellationToken = token}
            Parallel.ForEach(jpgsLocales, poptCopia, Sub(jpgLoc)
                If token.IsCancellationRequested Then Throw New OperationCanceledException()
                Dim nomArchivo = Path.GetFileName(jpgLoc)
                Dim rutaDestinoFinalLarga = APrefijoRutaLarga(Path.Combine(carpetaDestino, nomArchivo))

                Dim copiado As Boolean = False
                Dim ultimoEx As Exception = Nothing
                For intento As Integer = 1 To 4
                    Try
                        If Not Directory.Exists(carpetaDestinoLarga) Then
                            SyncLock lockerPaginas
                                If Not Directory.Exists(carpetaDestinoLarga) Then Directory.CreateDirectory(carpetaDestinoLarga)
                            End SyncLock
                        End If
                        File.Copy(jpgLoc, rutaDestinoFinalLarga, True)
                        copiado = True
                        Exit For
                    Catch exCopy As Exception
                        ultimoEx = exCopy
                        Thread.Sleep(150 * intento)
                        Try
                            Directory.CreateDirectory(carpetaDestinoLarga)
                        Catch
                        End Try
                    End Try
                Next

                If Not copiado Then
                    Throw New IOException($"Error al transferir '{nomArchivo}' a destino: " & If(ultimoEx IsNot Nothing, ultimoEx.Message, "Fallo de red SMB"), ultimoEx)
                End If
            End Sub)

            totalPaginas = jpgsLocales.Length

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
            If String.IsNullOrWhiteSpace(carpetaDestino) Then Exit Sub
            Dim carpetaLarga = APrefijoRutaLarga(carpetaDestino)
            If Not Directory.Exists(carpetaLarga) Then Exit Sub
            Dim archivos = Directory.GetFiles(carpetaLarga, "*.jpg")
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
            Dim rutaLarga = APrefijoRutaLarga(rutaArchivo)
            If Not File.Exists(rutaLarga) Then Return False
            Dim fi As New FileInfo(rutaLarga)
            If fi.Length < 32 Then Return False
            Using fs As New FileStream(rutaLarga, FileMode.Open, FileAccess.Read, FileShare.None)
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
            If String.IsNullOrWhiteSpace(carpetaDestino) Then Exit Sub
            Dim carpetaLarga = APrefijoRutaLarga(carpetaDestino)
            If Not Directory.Exists(carpetaLarga) Then Exit Sub
            Dim archivos = Directory.GetFiles(carpetaLarga, "*.*")
            For Each f In archivos
                Try
                    File.SetAttributes(f, FileAttributes.Normal)
                    File.Delete(f)
                Catch
                End Try
            Next
            Directory.Delete(carpetaLarga, True)
        Catch ex As Exception
            Debug.WriteLine("Error al limpiar carpeta incompleta: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Obtiene la ruta relativa respecto a la base.
    ''' </summary>
    Public Function ObtenerRutaRelativa(basePath As String, fullPath As String) As String
        If String.IsNullOrEmpty(basePath) OrElse String.IsNullOrEmpty(fullPath) Then Return fullPath
        Dim baseNorm = basePath.TrimEnd("\"c, "/"c) & "\"
        If fullPath.StartsWith(baseNorm, StringComparison.OrdinalIgnoreCase) Then
            Return fullPath.Substring(baseNorm.Length)
        End If
        Return Path.GetFileName(fullPath)
    End Function

    ''' <summary>
    ''' Normaliza carpetas tipo 'ENTREGA X' en 'ENTREGABLE X'.
    ''' </summary>
    Public Function NormalizarRutaRelativa(directorioRelativo As String) As String
        If String.IsNullOrWhiteSpace(directorioRelativo) Then Return ""
        If directorioRelativo.IndexOf("ENTREGA", StringComparison.OrdinalIgnoreCase) < 0 Then
            Return directorioRelativo
        End If
        Dim partes = directorioRelativo.Split(New Char() {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}, StringSplitOptions.None)
        For i As Integer = 0 To partes.Length - 1
            Dim m = Regex.Match(partes(i).Trim(), "^ENTREGA[\s_-]*(\d+.*)$", RegexOptions.IgnoreCase)
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

            Dim mEntrega = Regex.Match(nombreOrigen, "^ENTREGA[\s_-]*(\d+.*)$", RegexOptions.IgnoreCase)
            If mEntrega.Success Then
                subCarpetaDestino = "ENTREGABLE " & mEntrega.Groups(1).Value.Trim()
            End If

            Dim nombreDestino = Path.GetFileName(destino.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Trim()

            If nombreDestino.Equals(subCarpetaDestino, StringComparison.OrdinalIgnoreCase) OrElse
               nombreDestino.Equals(nombreOrigen, StringComparison.OrdinalIgnoreCase) Then
                Return destino
            End If

            Dim esEntregable = mEntrega.Success OrElse Regex.IsMatch(nombreOrigen, "^ENTREGABLE[\s_-]*(\d+.*)$", RegexOptions.IgnoreCase)
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
    Public Property CadenaConexionMysql As String = "server=127.0.0.1;port=3307;user id=appuser;password=1234;database=stellumdb;ConnectionTimeout=15;DefaultCommandTimeout=60;Pooling=true;MinPoolSize=2;MaxPoolSize=60;"

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
                Dim sql = $"SELECT estatus, paginas, error, fecha FROM {tabla} " &
                          "WHERE (documento = @nom1 OR documento = @nom2 OR nombre_original = @nom1 OR nombre_original = @nom2) " &
                          "ORDER BY CASE WHEN estatus = 'OK' THEN 0 ELSE 1 END ASC, id DESC LIMIT 1"
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
            End Using
        Catch ex As Exception
            info.ErrorMsg = ex.Message
        End Try
        Return info
    End Function

    ''' <summary>
    ''' Carga masivamente todos los registros de la bitácora JPG en un diccionario en memoria (O(1)).
    ''' Indexa tanto por ruta completa de carpeta destino / origen como por nombre de documento para evitar colisiones de homónimos.
    ''' </summary>
    Public Function CargarDiccionarioBitacoraEnMemoria() As Dictionary(Of String, InfoRegistroBd)
        Dim dict As New Dictionary(Of String, InfoRegistroBd)(StringComparer.OrdinalIgnoreCase)
        Try
            Using conn As New MySqlConnection(CadenaConexionMysql)
                conn.Open()
                Dim tabla = NombreTablaBitacora
                ' Ordenar de modo que los registros con estatus OK prevalezcan siempre al final
                Dim sql = $"SELECT documento, nombre_original, carpeta_origen, carpeta_destino, estatus, paginas, error FROM {tabla} " &
                          "ORDER BY CASE WHEN estatus = 'OK' THEN 1 ELSE 0 END ASC, id ASC"
                Using cmd As New MySqlCommand(sql, conn)
                    cmd.CommandTimeout = 120
                    Using dr = cmd.ExecuteReader()
                        While dr.Read()
                            Dim doc = If(IsDBNull(dr("documento")), "", dr("documento").ToString().Trim())
                            Dim orig = If(IsDBNull(dr("nombre_original")), "", dr("nombre_original").ToString().Trim())
                            Dim carpOrig = If(IsDBNull(dr("carpeta_origen")), "", dr("carpeta_origen").ToString().Trim())
                            Dim carpDest = If(IsDBNull(dr("carpeta_destino")), "", dr("carpeta_destino").ToString().Trim())
                            Dim info As New InfoRegistroBd With {
                                .Encontrado = True,
                                .Estatus = If(IsDBNull(dr("estatus")), "", dr("estatus").ToString().Trim()),
                                .Paginas = If(IsDBNull(dr("paginas")), 0, Convert.ToInt32(dr("paginas"))),
                                .ErrorMsg = If(IsDBNull(dr("error")), "", dr("error").ToString().Trim())
                            }

                            If Not String.IsNullOrEmpty(carpDest) Then
                                dict(carpDest.TrimEnd("\"c, "/"c)) = info
                            End If
                            If Not String.IsNullOrEmpty(carpOrig) AndAlso Not String.IsNullOrEmpty(orig) Then
                                Try
                                    dict(Path.Combine(carpOrig, Path.GetFileName(orig))) = info
                                Catch
                                End Try
                            End If
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
            Debug.WriteLine("Error al cargar diccionario masivo de BD (JPG): " & ex.Message)
        End Try
        Return dict
    End Function

    ''' <summary>
    ''' Registra o actualiza en la base de datos MySQL el archivo procesado/reparado con estatus OK en bitacora_transferencia_jpg.
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

                Dim sqlUpdate = $"UPDATE {tabla} SET " &
                                "estatus = 'OK', paginas = @pag, error = '', usuario = @usr, fecha = NOW(), " &
                                "carpeta_destino = @dest " &
                                "WHERE carpeta_destino = @dest " &
                                "OR (carpeta_origen = @origen AND (documento = @doc OR documento = @orig OR documento = @nuevo OR nombre_original = @orig OR nombre_original = @nuevo))"
                Dim filasActualizadas As Integer = 0
                Using cmdUpdate As New MySqlCommand(sqlUpdate, conn)
                    cmdUpdate.Parameters.AddWithValue("@pag", paginas)
                    cmdUpdate.Parameters.AddWithValue("@usr", usuario)
                    cmdUpdate.Parameters.AddWithValue("@dest", carpetaDestino)
                    cmdUpdate.Parameters.AddWithValue("@origen", carpetaOrigen)
                    cmdUpdate.Parameters.AddWithValue("@doc", documento)
                    cmdUpdate.Parameters.AddWithValue("@orig", nombreOriginal)
                    cmdUpdate.Parameters.AddWithValue("@nuevo", nombreNuevo)
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
                        cmdInsert.Parameters.AddWithValue("@anio", If(String.IsNullOrEmpty(anio), DBNull.Value, CObj(anio)))
                        cmdInsert.ExecuteNonQuery()
                    End Using
                End If
            End Using
        Catch ex As Exception
            ModuloBitacoraAsync.EncolarTransferenciaJpg(
                disk, carpetaOrigen, carpetaDestino, documento, nombreOriginal, nombreNuevo, "", "OK", paginas, delegacion, usuario, anio
            )
        End Try
    End Sub

    Public Class RegistroDetalleBd
        Public Property Id As Long = 0
        Public Property Documento As String = ""
        Public Property NombreOriginal As String = ""
        Public Property CarpetaOrigen As String = ""
        Public Property CarpetaDestino As String = ""
        Public Property Estatus As String = ""
        Public Property Paginas As Integer = 0
        Public Property Fecha As DateTime = DateTime.MinValue
    End Class

    ''' <summary>
    ''' Comprueba de forma exacta si un registro de BD pertenece realmente a la carpeta origen analizada,
    ''' evitando falsos positivos como 'ENTREGABLE 1' haciendo match con 'ENTREGABLE 10'.
    ''' </summary>
    Private Function PerteneceACarpetaOrigen(carpetaOrigenBd As String, diskBd As String, rutaOrigenCompleta As String, nombreCarpetaFinal As String) As Boolean
        Dim origNorm = rutaOrigenCompleta.Trim().TrimEnd("\"c, "/"c)

        If Not String.IsNullOrEmpty(carpetaOrigenBd) Then
            Dim cNorm = carpetaOrigenBd.Trim().TrimEnd("\"c, "/"c)
            If cNorm.Equals(origNorm, StringComparison.OrdinalIgnoreCase) OrElse
               cNorm.StartsWith(origNorm & "\", StringComparison.OrdinalIgnoreCase) OrElse
               cNorm.StartsWith(origNorm & "/", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
        End If

        If Not String.IsNullOrEmpty(diskBd) Then
            Dim dNorm = diskBd.Trim().TrimEnd("\"c, "/"c)
            If dNorm.Equals(origNorm, StringComparison.OrdinalIgnoreCase) OrElse
               dNorm.StartsWith(origNorm & "\", StringComparison.OrdinalIgnoreCase) OrElse
               dNorm.StartsWith(origNorm & "/", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
        End If

        ' Verificación por segmento exacto de ruta (para que 'ENTREGABLE 1' coincida solo con el segmento 'ENTREGABLE 1' y jamás con 'ENTREGABLE 10')
        If Not String.IsNullOrEmpty(nombreCarpetaFinal) Then
            Dim sep = New Char() {"\"c, "/"c}
            If Not String.IsNullOrEmpty(carpetaOrigenBd) Then
                Dim segmentos = carpetaOrigenBd.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                If segmentos.Any(Function(s) s.Trim().Equals(nombreCarpetaFinal, StringComparison.OrdinalIgnoreCase)) Then
                    Return True
                End If
            End If
            If Not String.IsNullOrEmpty(diskBd) Then
                Dim segmentosDisk = diskBd.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                If segmentosDisk.Any(Function(s) s.Trim().Equals(nombreCarpetaFinal, StringComparison.OrdinalIgnoreCase)) Then
                    Return True
                End If
            End If
        End If

        Return False
    End Function

    ''' <summary>
    ''' Obtiene todos los registros de la bitácora que coinciden exactamente con la carpeta origen (para análisis bidireccional y detección de huérfanos/duplicados).
    ''' </summary>
    Public Function ObtenerRegistrosBitacoraPorCarpeta(rutaCarpetaOrigen As String) As List(Of RegistroDetalleBd)
        Dim lista As New List(Of RegistroDetalleBd)()
        If String.IsNullOrWhiteSpace(rutaCarpetaOrigen) Then Return lista
        Try
            Dim origenNorm = rutaCarpetaOrigen.Trim().TrimEnd("\"c, "/"c)
            Dim nombreCarpeta = Path.GetFileName(origenNorm)
            Dim filtroSql = If(Not String.IsNullOrEmpty(nombreCarpeta), nombreCarpeta, origenNorm)

            Using conn As New MySqlConnection(CadenaConexionMysql)
                conn.Open()
                Dim tabla = NombreTablaBitacora
                Dim sql = $"SELECT id, documento, nombre_original, carpeta_origen, carpeta_destino, paginas, estatus, fecha " &
                          $"FROM {tabla} WHERE carpeta_origen LIKE @filtro OR disk LIKE @filtro"
                Using cmd As New MySqlCommand(sql, conn)
                    cmd.CommandTimeout = 120
                    cmd.Parameters.AddWithValue("@filtro", "%" & filtroSql & "%")
                    Using dr = cmd.ExecuteReader()
                        While dr.Read()
                            Dim carpOrig = If(IsDBNull(dr("carpeta_origen")), "", dr("carpeta_origen").ToString().Trim())
                            Dim diskVal = If(IsDBNull(dr("disk")), "", dr("disk").ToString().Trim())

                            If PerteneceACarpetaOrigen(carpOrig, diskVal, origenNorm, nombreCarpeta) Then
                                lista.Add(New RegistroDetalleBd With {
                                    .Id = Convert.ToInt64(dr("id")),
                                    .Documento = If(IsDBNull(dr("documento")), "", dr("documento").ToString().Trim()),
                                    .NombreOriginal = If(IsDBNull(dr("nombre_original")), "", dr("nombre_original").ToString().Trim()),
                                    .CarpetaOrigen = carpOrig,
                                    .CarpetaDestino = If(IsDBNull(dr("carpeta_destino")), "", dr("carpeta_destino").ToString().Trim()),
                                    .Paginas = If(IsDBNull(dr("paginas")), 0, Convert.ToInt32(dr("paginas"))),
                                    .Estatus = If(IsDBNull(dr("estatus")), "", dr("estatus").ToString().Trim()),
                                    .Fecha = If(IsDBNull(dr("fecha")), DateTime.MinValue, Convert.ToDateTime(dr("fecha")))
                                })
                            End If
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Debug.WriteLine("Error al obtener registros de bitácora por carpeta (JPG): " & ex.Message)
        End Try
        Return lista
    End Function

    ''' <summary>
    ''' Elimina un registro específico de la bitácora por su ID (usado para depurar duplicados o registros huérfanos que no existen en disco).
    ''' </summary>
    Public Function EliminarRegistroDeBitacoraPorId(idRegistro As Long) As Boolean
        If idRegistro <= 0 Then Return False
        Try
            Using conn As New MySqlConnection(CadenaConexionMysql)
                conn.Open()
                Dim tabla = NombreTablaBitacora
                Dim sql = $"DELETE FROM {tabla} WHERE id = @id"
                Using cmd As New MySqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@id", idRegistro)
                    Dim filas = cmd.ExecuteNonQuery()
                    If filas > 0 Then Return True
                End Using
            End Using
        Catch ex As Exception
            Debug.WriteLine($"Error al eliminar registro {idRegistro} de bitácora (JPG): " & ex.Message)
        End Try
        Return False
    End Function

End Module
