Imports System.IO
Imports System.Threading
Imports ImageMagick
Imports Newtonsoft.Json

''' <summary>
''' Módulo centralizado para detección dinámica de hardware, asignación inteligente de recursos
''' manteniendo un 10% de margen de seguridad (90% de uso de CPU y RAM), y gestión de búfer local.
''' </summary>
Public Module ModuloRecursos

    Public Class ConfiguracionRecursos
        Public Property ModoAutomatico As Boolean = True
        Public Property PorcentajeMargenLibre As Integer = 10 ' 10% libre para el sistema operativo
        Public Property HilosCpuManual As Integer = 8
        Public Property MemoriaRamManualGB As Integer = 16
        Public Property HilosRedConcurrente As Integer = 8
        Public Property RutaStagingLocal As String = "C:\MagickTempCache"
        Public Property UsarBufferLocal As Boolean = True
        Public Property RutaGhostscriptPersonalizada As String = ""
        Public Property RutaMuPdfPersonalizada As String = ""
    End Class

    Public Class InfoHardware
        Public Property NombreCpu As String = "Procesador no identificado"
        Public Property NucleosLogicos As Integer = 1
        Public Property TotalRamBytes As ULong = 0
        Public Property RamDisponibleBytes As ULong = 0
        Public Property HilosAsignados As Integer = 1
        Public Property MemoriaAsignadaBytes As ULong = 0
        Public Property MargenSeguridadPct As Integer = 10
        Public Property ModoAutomatico As Boolean = True
        Public Property RutaStaging As String = ""
        Public Property RutaGhostscript As String = ""
        Public Property RutaMuPdf As String = ""

        Public ReadOnly Property TotalRamGB As Double
            Get
                Return Math.Round(TotalRamBytes / (1024.0 * 1024.0 * 1024.0), 2)
            End Get
        End Property

        Public ReadOnly Property RamDisponibleGB As Double
            Get
                Return Math.Round(RamDisponibleBytes / (1024.0 * 1024.0 * 1024.0), 2)
            End Get
        End Property

        Public ReadOnly Property MemoriaAsignadaGB As Double
            Get
                Return Math.Round(MemoriaAsignadaBytes / (1024.0 * 1024.0 * 1024.0), 2)
            End Get
        End Property
    End Class

    Private Function ObtenerCarpetaBase() As String
        Try
            Dim loc = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            If Not String.IsNullOrEmpty(loc) AndAlso Directory.Exists(loc) Then Return loc
        Catch
        End Try
        Try
            If Not String.IsNullOrEmpty(Application.StartupPath) Then Return Application.StartupPath
        Catch
        End Try
        Return AppDomain.CurrentDomain.BaseDirectory
    End Function

    Private Function ObtenerRutaArchivoConfig() As String
        Return Path.Combine(ObtenerCarpetaBase(), "data", "config_recursos.json")
    End Function

    Private _configActual As ConfiguracionRecursos = Nothing

    Public Function ObtenerConfiguracion() As ConfiguracionRecursos
        If _configActual IsNot Nothing Then Return _configActual

        Dim archivoConfig = ObtenerRutaArchivoConfig()
        Try
            Dim carpetaData = Path.GetDirectoryName(archivoConfig)
            If Not Directory.Exists(carpetaData) Then Directory.CreateDirectory(carpetaData)

            If File.Exists(archivoConfig) Then
                Dim json = File.ReadAllText(archivoConfig)
                _configActual = JsonConvert.DeserializeObject(Of ConfiguracionRecursos)(json)
            End If
        Catch ex As Exception
            Debug.WriteLine("Error leyendo config_recursos: " & ex.Message)
        End Try

        If _configActual Is Nothing Then
            _configActual = New ConfiguracionRecursos()
            Dim hilosAuto = CalcularHilosOptimos(10)
            _configActual.HilosCpuManual = hilosAuto
            _configActual.MemoriaRamManualGB = Math.Max(2, CInt(Math.Floor(ObtenerTotalRamGB() * 0.9)))
            GuardarConfiguracion(_configActual, False)
        End If

        Return _configActual
    End Function

    Public Sub GuardarConfiguracion(cfg As ConfiguracionRecursos, Optional reaplicarLimites As Boolean = True)
        _configActual = cfg
        Try
            Dim archivoConfig = ObtenerRutaArchivoConfig()
            Dim carpetaData = Path.GetDirectoryName(archivoConfig)
            If Not Directory.Exists(carpetaData) Then Directory.CreateDirectory(carpetaData)

            Dim json = JsonConvert.SerializeObject(cfg, Formatting.Indented)
            File.WriteAllText(archivoConfig, json)
        Catch ex As Exception
            Debug.WriteLine("Error guardando config_recursos en disco: " & ex.Message)
        End Try

        If reaplicarLimites Then
            AplicarLimitesMagickNET()
        End If
    End Sub

    Public Function ObtenerHilosOptimos() As Integer
        Dim cfg = ObtenerConfiguracion()
        If Not cfg.ModoAutomatico AndAlso cfg.HilosCpuManual > 0 Then
            Return Math.Max(1, Math.Min(Environment.ProcessorCount, cfg.HilosCpuManual))
        End If
        Return CalcularHilosOptimos(cfg.PorcentajeMargenLibre)
    End Function

    Private Function CalcularHilosOptimos(margenLibrePct As Integer) As Integer
        Dim totalProcesadores = Environment.ProcessorCount
        Dim factorUso = (100.0 - Math.Max(5, Math.Min(50, margenLibrePct))) / 100.0
        Dim hilos = CInt(Math.Floor(totalProcesadores * factorUso))
        Return Math.Max(1, Math.Min(totalProcesadores - 1, hilos))
    End Function

    Public Function ObtenerTotalRamGB() As Double
        Try
            Return Math.Round(My.Computer.Info.TotalPhysicalMemory / (1024.0 * 1024.0 * 1024.0), 2)
        Catch
            Return 8.0
        End Try
    End Function

    Public Function ObtenerInfoHardwareCompleta() As InfoHardware
        Dim info As New InfoHardware()
        Try
            info.NucleosLogicos = Environment.ProcessorCount

            Try
                info.TotalRamBytes = My.Computer.Info.TotalPhysicalMemory
                info.RamDisponibleBytes = My.Computer.Info.AvailablePhysicalMemory
            Catch
                info.TotalRamBytes = 8UL * 1024UL * 1024UL * 1024UL
                info.RamDisponibleBytes = 4UL * 1024UL * 1024UL * 1024UL
            End Try

            ' Intentar obtener nombre comercial de la CPU desde el Registro de Windows
            Try
                Dim regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("HARDWARE\DESCRIPTION\System\CentralProcessor\0")
                If regKey IsNot Nothing Then
                    info.NombreCpu = regKey.GetValue("ProcessorNameString", "CPU Detectada").ToString().Trim()
                End If
            Catch
                info.NombreCpu = "Procesador de " & info.NucleosLogicos & " núcleos lógicos"
            End Try

            Dim cfg = ObtenerConfiguracion()
            info.ModoAutomatico = cfg.ModoAutomatico
            info.MargenSeguridadPct = cfg.PorcentajeMargenLibre
            info.HilosAsignados = ObtenerHilosOptimos()
            info.RutaStaging = ObtenerRutaStaging()

            If cfg.ModoAutomatico Then
                ' 90% de RAM disponible o total según configuración
                Dim factor = (100.0 - cfg.PorcentajeMargenLibre) / 100.0
                info.MemoriaAsignadaBytes = CULng(info.TotalRamBytes * factor)
            Else
                info.MemoriaAsignadaBytes = CULng(cfg.MemoriaRamManualGB) * 1024UL * 1024UL * 1024UL
            End If

            ' Protección de seguridad: si el proceso corre en 32 bits (x86), limitar la asignación a 1.5 GB
            ' para evitar desbordamiento de memoria virtual en ImageMagick nativo
            If IntPtr.Size = 4 Then
                info.MemoriaAsignadaBytes = Math.Min(info.MemoriaAsignadaBytes, 1536UL * 1024UL * 1024UL)
            End If

            ' Detección automática del motor de ultra-alto rendimiento Ghostscript CLI
            info.RutaGhostscript = ObtenerRutaEjecutableGhostscript()

            ' Detección automática del motor de velocidad extrema MuPDF CLI (mutool.exe)
            info.RutaMuPdf = ObtenerRutaEjecutableMuPdf()

        Catch ex As Exception
            Debug.WriteLine("Error en ObtenerInfoHardwareCompleta: " & ex.Message)
        End Try

        Return info
    End Function

    Public Function ObtenerRutaStaging() As String
        Dim cfg = ObtenerConfiguracion()
        Dim ruta = If(String.IsNullOrWhiteSpace(cfg.RutaStagingLocal), "", cfg.RutaStagingLocal.Trim())

        ' Intentar usar la ruta configurada si existe y es accesible
        If Not String.IsNullOrEmpty(ruta) Then
            Try
                If Not Directory.Exists(ruta) Then Directory.CreateDirectory(ruta)
                Dim testPath = Path.Combine(ruta, "staging_test.tmp")
                File.WriteAllText(testPath, "ok")
                File.Delete(testPath)
                Return ruta
            Catch
            End Try
        End If

        ' Fallback seguro a la carpeta temporal del usuario (siempre tiene permisos de escritura en Windows Server)
        Dim fallback = Path.Combine(Path.GetTempPath(), "ValidacionStaging")
        Try
            If Not Directory.Exists(fallback) Then Directory.CreateDirectory(fallback)
            Return fallback
        Catch
            Return Path.GetTempPath()
        End Try
    End Function

    Public Sub AplicarLimitesMagickNET()
        Try
            Dim info = ObtenerInfoHardwareCompleta()
            Dim rutaStaging = ObtenerRutaStaging()

            ' Configurar memoria límite en ImageMagick (90% configurado)
            Try
                If info.MemoriaAsignadaBytes > 0 Then
                    ResourceLimits.Memory = info.MemoriaAsignadaBytes
                    ResourceLimits.Area = info.MemoriaAsignadaBytes
                End If
                ' PROHIBIR terminantemente que ImageMagick cree archivos de caché de píxeles (.cache) en el disco duro.
                ' Todo el procesamiento debe residir estrictamente en la memoria RAM del servidor.
                ResourceLimits.Disk = 0UL
            Catch ex As Exception
                Debug.WriteLine("Advertencia al asignar ResourceLimits.Memory: " & ex.Message)
            End Try

            ' Establecer el directorio temporal para caché de imágenes
            Try
                MagickNET.SetTempDirectory(rutaStaging)
            Catch ex As Exception
                Debug.WriteLine("Advertencia al asignar SetTempDirectory: " & ex.Message)
            End Try

            Debug.WriteLine($"[ModuloRecursos] Magick.NET configurado: {info.MemoriaAsignadaGB} GB RAM, Staging: {rutaStaging}, Hilos recomendados: {info.HilosAsignados}")
        Catch ex As Exception
            Debug.WriteLine("Error al aplicar límites de MagickNET: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Limpia archivos residuales y huérfanos de ejecuciones previas (magick-*, gs_*, mu_*, stg_*, .tmp, .cache)
    ''' en la carpeta de staging para liberar espacio inmediatamente.
    ''' </summary>
    Public Sub LimpiarCacheHuerfana()
        Try
            Dim rutaStaging = ObtenerRutaStaging()
            If Directory.Exists(rutaStaging) Then
                Dim dirInfo As New DirectoryInfo(rutaStaging)

                ' Eliminar subcarpetas huérfanas temporales de ejecuciones anteriores
                For Each subDir In dirInfo.GetDirectories()
                    Try
                        If subDir.Name.StartsWith("stg_", StringComparison.OrdinalIgnoreCase) OrElse
                           subDir.Name.StartsWith("gs_", StringComparison.OrdinalIgnoreCase) OrElse
                           subDir.Name.StartsWith("mu_", StringComparison.OrdinalIgnoreCase) Then
                            subDir.Delete(True)
                        End If
                    Catch
                    End Try
                Next

                ' Eliminar archivos de caché huérfanos (.cache, .tmp, magick-*)
                For Each fi In dirInfo.GetFiles()
                    Try
                        If fi.Name.StartsWith("magick-", StringComparison.OrdinalIgnoreCase) OrElse
                           fi.Extension.Equals(".cache", StringComparison.OrdinalIgnoreCase) OrElse
                           fi.Extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase) OrElse
                           fi.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) Then
                            fi.Delete()
                        End If
                    Catch
                    End Try
                Next
            End If
        Catch ex As Exception
            Debug.WriteLine("Error en LimpiarCacheHuerfana: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Límite por defecto para clasificar un PDF como grande (30 MB).
    ''' PDFs mayores a este tamaño se procesan 1 a la vez utilizando los 100 hilos para sus páginas.
    ''' </summary>
    Public Const LimitePdfGrandeBytes As Long = 30L * 1024L * 1024L

    ''' <summary>
    ''' Obtiene el espacio libre disponible en Gigabytes para el disco donde reside la ruta especificada.
    ''' </summary>
    Public Function ObtenerEspacioLibreDiscoGB(rutaDirectorio As String) As Double
        Try
            Dim full = Path.GetFullPath(rutaDirectorio)
            Dim root = Path.GetPathRoot(full)
            If Not String.IsNullOrEmpty(root) Then
                Dim drive As New DriveInfo(root)
                Return Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2)
            End If
        Catch
        End Try
        Return 999.0
    End Function

    ''' <summary>
    ''' Comprueba si el disco de staging tiene al menos el margen mínimo de GB libres.
    ''' </summary>
    Public Function HayEspacioSuficienteStaging(Optional minimoRequeridoGB As Double = 15.0) As Boolean
        Dim rutaStaging = ObtenerRutaStaging()
        Dim libre = ObtenerEspacioLibreDiscoGB(rutaStaging)
        Return libre >= minimoRequeridoGB
    End Function

    ''' <summary>
    ''' Espera activamente a que los hilos en vuelo liberen espacio en disco si se llega a una situación crítica.
    ''' </summary>
    Public Function EsperarEspacioDisponible(Optional minimoRequeridoGB As Double = 15.0, Optional token As CancellationToken = Nothing, Optional timeoutMs As Integer = 30000) As Boolean
        Dim sw = Diagnostics.Stopwatch.StartNew()
        While Not HayEspacioSuficienteStaging(minimoRequeridoGB)
            If token.IsCancellationRequested Then Return False
            If sw.ElapsedMilliseconds > timeoutMs Then Return False
            Thread.Sleep(500)
        End While
        Return True
    End Function

    Private _rutaGhostscriptCache As String = Nothing
    Private _ghostscriptVerificado As Boolean = False

    ''' <summary>
    ''' Localiza el ejecutable de consola de Ghostscript (gswin64c.exe o gswin32c.exe)
    ''' en el sistema: configuración personalizada, variables de entorno, Program Files, Registro de Windows o PATH.
    ''' </summary>
    Public Function ObtenerRutaEjecutableGhostscript(Optional forzarReevaluacion As Boolean = False) As String
        If Not forzarReevaluacion AndAlso _ghostscriptVerificado Then
            Return _rutaGhostscriptCache
        End If

        ' 1. Configuración explícita en config_recursos.json
        Dim cfg = ObtenerConfiguracion()
        If Not String.IsNullOrWhiteSpace(cfg.RutaGhostscriptPersonalizada) Then
            Dim rCustom = cfg.RutaGhostscriptPersonalizada.Trim()
            If File.Exists(rCustom) Then
                _rutaGhostscriptCache = rCustom
                _ghostscriptVerificado = True
                Return _rutaGhostscriptCache
            ElseIf Directory.Exists(rCustom) Then
                Dim c64 = Path.Combine(rCustom, "gswin64c.exe")
                If File.Exists(c64) Then
                    _rutaGhostscriptCache = c64
                    _ghostscriptVerificado = True
                    Return _rutaGhostscriptCache
                End If
                Dim c32 = Path.Combine(rCustom, "gswin32c.exe")
                If File.Exists(c32) Then
                    _rutaGhostscriptCache = c32
                    _ghostscriptVerificado = True
                    Return _rutaGhostscriptCache
                End If
            End If
        End If

        ' 2. Variables de entorno GS_BIN, GHOSTSCRIPT_PATH, GS_PATH
        Dim envNames = {"GS_BIN", "GHOSTSCRIPT_PATH", "GS_PATH"}
        For Each envName In envNames
            Dim envVal = Environment.GetEnvironmentVariable(envName)
            If Not String.IsNullOrWhiteSpace(envVal) Then
                Dim val = envVal.Trim()
                If File.Exists(val) Then
                    _rutaGhostscriptCache = val
                    _ghostscriptVerificado = True
                    Return _rutaGhostscriptCache
                ElseIf Directory.Exists(val) Then
                    Dim c64 = Path.Combine(val, "gswin64c.exe")
                    If File.Exists(c64) Then
                        _rutaGhostscriptCache = c64
                        _ghostscriptVerificado = True
                        Return _rutaGhostscriptCache
                    End If
                    Dim c64bin = Path.Combine(val, "bin", "gswin64c.exe")
                    If File.Exists(c64bin) Then
                        _rutaGhostscriptCache = c64bin
                        _ghostscriptVerificado = True
                        Return _rutaGhostscriptCache
                    End If
                    Dim c32 = Path.Combine(val, "gswin32c.exe")
                    If File.Exists(c32) Then
                        _rutaGhostscriptCache = c32
                        _ghostscriptVerificado = True
                        Return _rutaGhostscriptCache
                    End If
                End If
            End If
        Next

        ' 3. Carpetas estándar en Program Files (64 y 32 bits)
        Dim standardRoots = {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "C:\Program Files",
            "C:\Program Files (x86)",
            "D:\Program Files",
            "D:\gs",
            "C:\gs"
        }

        For Each root In standardRoots
            Try
                If String.IsNullOrEmpty(root) OrElse Not Directory.Exists(root) Then Continue For
                Dim gsDir = If(root.EndsWith("gs", StringComparison.OrdinalIgnoreCase), root, Path.Combine(root, "gs"))
                If Directory.Exists(gsDir) Then
                    ' Buscar subdirectorios gs* ordenados de mayor versión a menor
                    Dim subdirs = Directory.GetDirectories(gsDir).OrderByDescending(Function(d) d).ToList()
                    For Each subd In subdirs
                        Dim exe64 = Path.Combine(subd, "bin", "gswin64c.exe")
                        If File.Exists(exe64) Then
                            _rutaGhostscriptCache = exe64
                            _ghostscriptVerificado = True
                            Return _rutaGhostscriptCache
                        End If
                        Dim exe32 = Path.Combine(subd, "bin", "gswin32c.exe")
                        If File.Exists(exe32) Then
                            _rutaGhostscriptCache = exe32
                            _ghostscriptVerificado = True
                            Return _rutaGhostscriptCache
                        End If
                    Next
                End If
            Catch
            End Try
        Next

        ' 4. Registro de Windows (HKLM y HKCU)
        Dim regBases = {
            Microsoft.Win32.Registry.LocalMachine,
            Microsoft.Win32.Registry.CurrentUser
        }
        Dim regSubPaths = {
            "SOFTWARE\GPL Ghostscript",
            "SOFTWARE\Artifex\Ghostscript",
            "SOFTWARE\Ghostscript"
        }

        For Each regBase In regBases
            For Each subPath In regSubPaths
                Try
                    Using key = regBase.OpenSubKey(subPath)
                        If key IsNot Nothing Then
                            Dim versions = key.GetSubKeyNames().OrderByDescending(Function(v) v).ToList()
                            For Each ver In versions
                                Using verKey = key.OpenSubKey(ver)
                                    If verKey IsNot Nothing Then
                                        Dim gsDll = verKey.GetValue("GS_DLL")
                                        Dim gsDllStr = If(gsDll IsNot Nothing, gsDll.ToString(), "")
                                        If Not String.IsNullOrEmpty(gsDllStr) AndAlso File.Exists(gsDllStr) Then
                                            Dim binDir = Path.GetDirectoryName(gsDllStr)
                                            Dim exe64 = Path.Combine(binDir, "gswin64c.exe")
                                            If File.Exists(exe64) Then
                                                _rutaGhostscriptCache = exe64
                                                _ghostscriptVerificado = True
                                                Return _rutaGhostscriptCache
                                            End If
                                            Dim exe32 = Path.Combine(binDir, "gswin32c.exe")
                                            If File.Exists(exe32) Then
                                                _rutaGhostscriptCache = exe32
                                                _ghostscriptVerificado = True
                                                Return _rutaGhostscriptCache
                                            End If
                                        End If
                                    End If
                                End Using
                            Next
                        End If
                    End Using
                Catch
                End Try
            Next
        Next

        ' 5. PATH de Windows
        Try
            Dim pathEnv = Environment.GetEnvironmentVariable("PATH")
            If Not String.IsNullOrEmpty(pathEnv) Then
                For Each p In pathEnv.Split(";"c)
                    Dim trimmed = p.Trim()
                    If Not String.IsNullOrEmpty(trimmed) AndAlso Directory.Exists(trimmed) Then
                        Dim test64 = Path.Combine(trimmed, "gswin64c.exe")
                        If File.Exists(test64) Then
                            _rutaGhostscriptCache = test64
                            _ghostscriptVerificado = True
                            Return _rutaGhostscriptCache
                        End If
                        Dim test32 = Path.Combine(trimmed, "gswin32c.exe")
                        If File.Exists(test32) Then
                            _rutaGhostscriptCache = test32
                            _ghostscriptVerificado = True
                            Return _rutaGhostscriptCache
                        End If
                        Dim testGs = Path.Combine(trimmed, "gs.exe")
                        If File.Exists(testGs) Then
                            _rutaGhostscriptCache = testGs
                            _ghostscriptVerificado = True
                            Return _rutaGhostscriptCache
                        End If
                    End If
                Next
            End If
        Catch
        End Try

        _ghostscriptVerificado = True
        _rutaGhostscriptCache = Nothing
        Return Nothing
    End Function

    Private _rutaMuPdfCache As String = Nothing
    Private _muPdfVerificado As Boolean = False

    ''' <summary>
    ''' Localiza el ejecutable de consola de MuPDF (mutool.exe)
    ''' en el sistema: configuración personalizada, subcarpeta tools\ del programa,
    ''' variables de entorno, Program Files o PATH.
    ''' </summary>
    Public Function ObtenerRutaEjecutableMuPdf(Optional forzarReevaluacion As Boolean = False) As String
        If Not forzarReevaluacion AndAlso _muPdfVerificado Then
            Return _rutaMuPdfCache
        End If

        ' 1. Configuración explícita en config_recursos.json
        Dim cfg = ObtenerConfiguracion()
        If Not String.IsNullOrWhiteSpace(cfg.RutaMuPdfPersonalizada) Then
            Dim rCustom = cfg.RutaMuPdfPersonalizada.Trim()
            If File.Exists(rCustom) Then
                _rutaMuPdfCache = rCustom
                _muPdfVerificado = True
                Return _rutaMuPdfCache
            ElseIf Directory.Exists(rCustom) Then
                Dim cMu = Path.Combine(rCustom, "mutool.exe")
                If File.Exists(cMu) Then
                    _rutaMuPdfCache = cMu
                    _muPdfVerificado = True
                    Return _rutaMuPdfCache
                End If
            End If
        End If

        ' 2. Carpeta local del programa (portabilidad directa sin instalación)
        Dim baseDir = ObtenerCarpetaBase()
        Dim localPaths = {
            Path.Combine(baseDir, "tools", "mutool.exe"),
            Path.Combine(baseDir, "bin", "mutool.exe"),
            Path.Combine(baseDir, "mutool.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "mutool.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mutool.exe")
        }
        For Each lp In localPaths
            If File.Exists(lp) Then
                _rutaMuPdfCache = Path.GetFullPath(lp)
                _muPdfVerificado = True
                Return _rutaMuPdfCache
            End If
        Next

        ' 3. Variables de entorno MUPDF_PATH, MUTOOL_PATH
        Dim envNames = {"MUPDF_PATH", "MUTOOL_PATH"}
        For Each envName In envNames
            Dim envVal = Environment.GetEnvironmentVariable(envName)
            If Not String.IsNullOrWhiteSpace(envVal) Then
                Dim val = envVal.Trim()
                If File.Exists(val) Then
                    _rutaMuPdfCache = val
                    _muPdfVerificado = True
                    Return _rutaMuPdfCache
                ElseIf Directory.Exists(val) Then
                    Dim cMu = Path.Combine(val, "mutool.exe")
                    If File.Exists(cMu) Then
                        _rutaMuPdfCache = cMu
                        _muPdfVerificado = True
                        Return _rutaMuPdfCache
                    End If
                End If
            End If
        Next

        ' 4. Carpetas estándar en Program Files
        Dim standardRoots = {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "C:\Program Files\mupdf",
            "C:\tools\mupdf",
            "C:\mupdf"
        }
        For Each root In standardRoots
            Try
                If String.IsNullOrEmpty(root) OrElse Not Directory.Exists(root) Then Continue For
                Dim directExe = Path.Combine(root, "mutool.exe")
                If File.Exists(directExe) Then
                    _rutaMuPdfCache = directExe
                    _muPdfVerificado = True
                    Return _rutaMuPdfCache
                End If
                Dim subExe = Path.Combine(root, "mupdf", "mutool.exe")
                If File.Exists(subExe) Then
                    _rutaMuPdfCache = subExe
                    _muPdfVerificado = True
                    Return _rutaMuPdfCache
                End If
            Catch
            End Try
        Next

        ' 5. PATH de Windows
        Try
            Dim pathEnv = Environment.GetEnvironmentVariable("PATH")
            If Not String.IsNullOrEmpty(pathEnv) Then
                For Each p In pathEnv.Split(";"c)
                    Dim trimmed = p.Trim()
                    If Not String.IsNullOrEmpty(trimmed) AndAlso Directory.Exists(trimmed) Then
                        Dim testMu = Path.Combine(trimmed, "mutool.exe")
                        If File.Exists(testMu) Then
                            _rutaMuPdfCache = testMu
                            _muPdfVerificado = True
                            Return _rutaMuPdfCache
                        End If
                    End If
                Next
            End If
        Catch
        End Try

        _muPdfVerificado = True
        _rutaMuPdfCache = Nothing
        Return Nothing
    End Function

End Module
