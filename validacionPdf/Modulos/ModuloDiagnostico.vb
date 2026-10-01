Imports System.IO
Imports System.Reflection
Imports System.Text

''' <summary>
''' Módulo de diagnóstico y registro exhaustivo para identificar cualquier fallo,
''' congelamiento o dependencia faltante tanto en entornos locales como en servidores de producción.
''' </summary>
Public Module ModuloDiagnostico

    Private _rutaLog As String = ""
    Private ReadOnly _locker As New Object()
    Private _inicializado As Boolean = False

    ''' <summary>
    ''' Inicializa el sistema de captura de logs y los manejadores globales de excepciones.
    ''' </summary>
    Public Sub InicializarDiagnostico()
        SyncLock _locker
            If _inicializado Then Return
            _inicializado = True

            ' 1. Determinar ruta del log con fallbacks en cascada si no hay permisos de escritura
            _rutaLog = DeterminarRutaLog()

            ' 2. Registrar manejadores de excepciones no controladas a nivel de AppDomain, Hilo y Tareas
            Try
                AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnUnhandledException
                AddHandler Application.ThreadException, AddressOf OnThreadException
                AddHandler TaskScheduler.UnobservedTaskException, AddressOf OnUnobservedTaskException
            Catch ex As Exception
                Debug.WriteLine("Error al conectar eventos de diagnóstico: " & ex.Message)
            End Try

            ' 3. Escribir encabezado de diagnóstico inicial
            Dim sb As New StringBuilder()
            sb.AppendLine("================================================================================")
            sb.AppendLine($" REGISTRO DE DEPURACIÓN Y DIAGNÓSTICO DEL SISTEMA - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}")
            sb.AppendLine("================================================================================")
            sb.AppendLine($"Archivo de log: {_rutaLog}")
            sb.AppendLine($"Equipo (Host): {Environment.MachineName}")
            sb.AppendLine($"Sistema Operativo: {Environment.OSVersion} ({(If(Environment.Is64BitOperatingSystem, "64 bits", "32 bits"))})")
            sb.AppendLine($"Proceso en ejecución: {(If(Environment.Is64BitProcess, "Nativo 64 bits (x64)", "32 bits WOW64 (x86)"))}")
            sb.AppendLine($"Versión CLR .NET: {Environment.Version}")
            sb.AppendLine($"Núcleos lógicos (ProcessorCount): {Environment.ProcessorCount}")
            sb.AppendLine($"Directorio base (.exe): {AppDomain.CurrentDomain.BaseDirectory}")
            sb.AppendLine($"Directorio de trabajo: {Environment.CurrentDirectory}")
            sb.AppendLine($"Usuario actual: {Environment.UserName}")
            sb.AppendLine($"Sesión interactiva: {Environment.UserInteractive}")

            ' Diagnóstico de Memoria RAM
            Try
                Dim totalRamBytes = My.Computer.Info.TotalPhysicalMemory
                Dim availRamBytes = My.Computer.Info.AvailablePhysicalMemory
                Dim totalRamGB = Math.Round(totalRamBytes / (1024.0 * 1024.0 * 1024.0), 2)
                Dim availRamGB = Math.Round(availRamBytes / (1024.0 * 1024.0 * 1024.0), 2)
                sb.AppendLine($"Memoria RAM Total: {totalRamGB} GB ({totalRamBytes:N0} bytes)")
                sb.AppendLine($"Memoria RAM Disponible: {availRamGB} GB ({availRamBytes:N0} bytes)")
            Catch exRam As Exception
                sb.AppendLine($"Aviso al leer memoria RAM: {exRam.Message}")
            End Try

            ' Verificación de archivos DLL esenciales en la carpeta del ejecutable
            sb.AppendLine("--------------------------------------------------------------------------------")
            sb.AppendLine(" VERIFICACIÓN DE DEPENDENCIAS Y DLLS ESENCIALES:")
            Dim dllsRequeridas As String() = {
                "Newtonsoft.Json.dll",
                "Magick.NET-Q8-AnyCPU.dll",
                "Magick.Native-Q8-x64.dll",
                "Magick.NET.Core.dll",
                "MySqlConnector.dll",
                "ClosedXML.dll",
                "DocumentFormat.OpenXml.dll",
                "itext.kernel.dll",
                "validacionPdf.exe.config"
            }

            Dim baseDir = AppDomain.CurrentDomain.BaseDirectory
            Dim faltantes As New List(Of String)()
            For Each dll In dllsRequeridas
                Dim rutaDll = Path.Combine(baseDir, dll)
                If File.Exists(rutaDll) Then
                    Dim fi As New FileInfo(rutaDll)
                    sb.AppendLine($"  [OK] {dll} ({fi.Length:N0} bytes)")
                Else
                    sb.AppendLine($"  [FALTANTE] {dll} -> NO ENCONTRADA EN {baseDir}")
                    faltantes.Add(dll)
                End If
            Next
            sb.AppendLine("--------------------------------------------------------------------------------")

            EscribirDirecto(sb.ToString())

            If faltantes.Count > 0 Then
                Dim msgFaltantes = "¡ATENCIÓN! Faltan archivos DLL esenciales en la carpeta de ejecución:" & vbCrLf &
                                   String.Join(vbCrLf, faltantes) & vbCrLf & vbCrLf &
                                   "Asegúrese de copiar la carpeta 'bin\Release' completa al servidor, no solo el archivo .exe."
                MessageBox.Show(msgFaltantes, "Dependencias Faltantes", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Registra un mensaje con marca de tiempo precisa e identificador de hilo.
    ''' </summary>
    Public Sub Log(mensaje As String)
        Try
            Dim tid = System.Threading.Thread.CurrentThread.ManagedThreadId
            Dim linea = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Hilo #{tid}] {mensaje}"
            EscribirDirecto(linea & Environment.NewLine)
            Debug.WriteLine(linea)
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Registra un error o excepción de forma detallada y opcionalmente lo muestra en pantalla.
    ''' </summary>
    Public Sub LogError(titulo As String, ex As Exception, Optional mostrarMsgBox As Boolean = True)
        Try
            Dim tid = System.Threading.Thread.CurrentThread.ManagedThreadId
            Dim sb As New StringBuilder()
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Hilo #{tid}] ERROR CRÍTICO: {titulo}")
            If ex IsNot Nothing Then
                sb.AppendLine($"  Tipo: {ex.GetType().FullName}")
                sb.AppendLine($"  Mensaje: {ex.Message}")
                sb.AppendLine($"  StackTrace:{Environment.NewLine}{ex.StackTrace}")
                Dim inner = ex.InnerException
                While inner IsNot Nothing
                    sb.AppendLine($"  --> Causa interna: {inner.GetType().FullName}: {inner.Message}")
                    sb.AppendLine($"      StackTrace:{Environment.NewLine}{inner.StackTrace}")
                    inner = inner.InnerException
                End While
            End If
            EscribirDirecto(sb.ToString())

            If mostrarMsgBox AndAlso Environment.UserInteractive Then
                Dim mensajeVisual = $"{titulo}:{vbCrLf}{If(ex IsNot Nothing, ex.Message, "Error desconocido")}{vbCrLf}{vbCrLf}Revise el archivo de registro en:{vbCrLf}{_rutaLog}"
                MessageBox.Show(mensajeVisual, "Error en Servidor", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Devuelve la ruta absoluta del archivo de registro actual.
    ''' </summary>
    Public Function ObtenerRutaLog() As String
        If String.IsNullOrEmpty(_rutaLog) Then
            _rutaLog = DeterminarRutaLog()
        End If
        Return _rutaLog
    End Function

    Private Function DeterminarRutaLog() As String
        ' Opción 1: Junto al ejecutable
        Try
            Dim p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "depuracion_servidor.log")
            File.AppendAllText(p1, "")
            Return p1
        Catch
        End Try

        ' Opción 2: En el Escritorio del usuario
        Try
            Dim escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            If Directory.Exists(escritorio) Then
                Dim p2 = Path.Combine(escritorio, "depuracion_servidor.log")
                File.AppendAllText(p2, "")
                Return p2
            End If
        Catch
        End Try

        ' Opción 3: En la carpeta temporal de Windows
        Try
            Dim p3 = Path.Combine(Path.GetTempPath(), "depuracion_servidor.log")
            File.AppendAllText(p3, "")
            Return p3
        Catch
        End Try

        Return "depuracion_servidor.log"
    End Function

    Private Sub EscribirDirecto(texto As String)
        SyncLock _locker
            Try
                If String.IsNullOrEmpty(_rutaLog) Then _rutaLog = DeterminarRutaLog()
                File.AppendAllText(_rutaLog, texto, Encoding.UTF8)
            Catch
            End Try
        End SyncLock
    End Sub

    Private Sub OnUnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
        Dim ex = TryCast(e.ExceptionObject, Exception)
        LogError("Excepción no controlada a nivel de AppDomain", ex, True)
    End Sub

    Private Sub OnThreadException(sender As Object, e As System.Threading.ThreadExceptionEventArgs)
        LogError("Excepción no controlada en Hilo de Interfaz (ThreadException)", e.Exception, True)
    End Sub

    Private Sub OnUnobservedTaskException(sender As Object, e As Threading.Tasks.UnobservedTaskExceptionEventArgs)
        LogError("Excepción no observada en Tarea asíncrona (TaskException)", e.Exception, False)
        e.SetObserved()
    End Sub

End Module
