Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Punto de entrada principal explícito de la aplicación.
''' Garantiza la captura de diagnósticos desde la primera línea de código
''' y un bucle de mensajes nativo (Application.Run) que evita el estado 'Suspendido' en servidores.
''' </summary>
Public Module Program

    <STAThread>
    Public Sub Main(args As String())
        ' 1. Inicializar diagnóstico inmediatamente en la primera línea de ejecución
        ModuloDiagnostico.InicializarDiagnostico()
        ModuloDiagnostico.Log("================================================================================")
        ModuloDiagnostico.Log(" INICIO DEL PROCESO - Program.Main()")
        ModuloDiagnostico.Log("================================================================================")

        Try
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            ModuloDiagnostico.Log("Estilos visuales y renderizado de texto configurados.")

            ' 2. Inicializar catálogo de usuarios
            ModuloDiagnostico.Log("Ejecutando ModuloUsuarios.InicializarDatos()...")
            ModuloUsuarios.InicializarDatos()
            ModuloDiagnostico.Log("ModuloUsuarios.InicializarDatos completado.")

            ' 3. Configuración de recursos de hardware en segundo plano
            ModuloDiagnostico.Log("Iniciando tarea en segundo plano para límites de recursos de hardware...")
            Threading.Tasks.Task.Run(Sub()
                                         Try
                                             ModuloRecursos.AplicarLimitesMagickNET()
                                             ModuloDiagnostico.Log("Recursos de hardware y Magick.NET configurados.")
                                         Catch exRecursos As Exception
                                             ModuloDiagnostico.LogError("Aviso en configuración de hardware en segundo plano", exRecursos, False)
                                         End Try
                                     End Sub)

            ' 4. Iniciar la aplicación con FormLogin como ventana principal en el bucle de mensajes
            ModuloDiagnostico.Log("Creando e iniciando FormLogin en Application.Run()...")
            Using frmLogin As New FormLogin()
                frmLogin.ShowInTaskbar = True
                frmLogin.StartPosition = FormStartPosition.CenterScreen
                Application.Run(frmLogin)
            End Using

            ModuloDiagnostico.Log("FormLogin cerrado. El proceso finaliza normalmente.")

        Catch exFatal As Exception
            ModuloDiagnostico.LogError("ERROR FATAL en Program.Main", exFatal, True)
        Finally
            Try
                ModuloDirectorios.LiberarLoteActual()
                ModuloBitacoraAsync.EsperarVaciado(3000)
            Catch
            End Try
            ModuloDiagnostico.Log("=== FIN DE LA EJECUCIÓN DEL PROCESO ===")
        End Try
    End Sub

End Module
