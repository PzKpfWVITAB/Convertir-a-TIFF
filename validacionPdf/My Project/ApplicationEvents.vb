Imports Microsoft.VisualBasic.ApplicationServices

Namespace My

    ''' <summary>
    ''' Extensión de MyApplication para mostrar el login antes de Form1.
    ''' </summary>
    Partial Friend Class MyApplication

        Private Sub MyApplication_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
            Try
                ' 0. Inicializar sistema de diagnóstico y registro en servidor
                ModuloDiagnostico.InicializarDiagnostico()
                ModuloDiagnostico.Log("1. Evento MyApplication_Startup disparado.")

                ' 1. Inicializar catálogo de usuarios
                ModuloDiagnostico.Log("2. Ejecutando ModuloUsuarios.InicializarDatos()...")
                ModuloUsuarios.InicializarDatos()
                ModuloDiagnostico.Log("2.1 ModuloUsuarios.InicializarDatos completado.")

                ' 2. Aplicar límites de hardware y configuración de ImageMagick en segundo plano
                ModuloDiagnostico.Log("3. Lanzando tarea de configuración de recursos de hardware...")
                System.Threading.Tasks.Task.Run(Sub()
                                                    Try
                                                        ModuloDiagnostico.Log("3.1 Tarea en segundo plano: Ejecutando ModuloRecursos.AplicarLimitesMagickNET()...")
                                                        ModuloRecursos.AplicarLimitesMagickNET()
                                                        ModuloDiagnostico.Log("3.2 Tarea en segundo plano: Magick.NET configurado exitosamente.")
                                                    Catch exRecursos As Exception
                                                        ModuloDiagnostico.LogError("Error al configurar recursos en segundo plano", exRecursos, False)
                                                    End Try
                                                End Sub)

                ' 3. Mostrar pantalla de inicio de sesión
                ModuloDiagnostico.Log("4. Instanciando FormLogin...")
                Using frmLogin As New FormLogin()
                    frmLogin.ShowInTaskbar = True
                    frmLogin.StartPosition = FormStartPosition.CenterScreen
                    ModuloDiagnostico.Log("4.1 Llamando a frmLogin.ShowDialog()...")
                    Dim resultadoLogin = frmLogin.ShowDialog()
                    ModuloDiagnostico.Log($"4.2 frmLogin.ShowDialog() finalizado con resultado: {resultadoLogin}")

                    If resultadoLogin <> DialogResult.OK Then
                        ModuloDiagnostico.Log("4.3 Login cancelado o cerrado por el usuario. Cancelando inicio de la aplicación.")
                        e.Cancel = True
                        Exit Sub
                    End If
                End Using

                ' 4. Mostrar selección de directorio y lote (para flujo principal de validación)
                ModuloDiagnostico.Log("5. Instanciando FormSeleccionSesion...")
                Using frmSesion As New FormSeleccionSesion()
                    frmSesion.ShowInTaskbar = True
                    frmSesion.StartPosition = FormStartPosition.CenterScreen
                    ModuloDiagnostico.Log("5.1 Llamando a frmSesion.ShowDialog()...")
                    Dim resultadoSesion = frmSesion.ShowDialog()
                    ModuloDiagnostico.Log($"5.2 frmSesion.ShowDialog() finalizado con resultado: {resultadoSesion}")

                    If resultadoSesion <> DialogResult.OK Then
                        ModuloDiagnostico.Log("5.3 Selección de sesión cancelada. Cancelando inicio de la aplicación.")
                        e.Cancel = True
                        Exit Sub
                    End If
                End Using

                ModuloDiagnostico.Log("6. Startup completado con éxito. Creando formulario principal (Form1)...")

            Catch ex As Exception
                ModuloDiagnostico.LogError("Error durante el inicio del sistema (MyApplication_Startup)", ex, True)
                e.Cancel = True
            End Try
        End Sub

        Private Sub MyApplication_Shutdown(sender As Object, e As EventArgs) Handles Me.Shutdown
            ModuloDiagnostico.Log("7. Evento MyApplication_Shutdown iniciado.")
            Try
                ModuloDirectorios.LiberarLoteActual()
                ModuloBitacoraAsync.EsperarVaciado(3000)
                ModuloDiagnostico.Log("7.1 MyApplication_Shutdown finalizado limpiamente.")
            Catch ex As Exception
                ModuloDiagnostico.LogError("Error durante el cierre de la aplicación", ex, False)
            End Try
        End Sub

        Private Sub MyApplication_UnhandledException(sender As Object, e As Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs) Handles Me.UnhandledException
            ModuloDiagnostico.LogError("Excepción global no controlada (MyApplication_UnhandledException)", e.Exception, True)
            e.ExitApplication = True
        End Sub

    End Class

End Namespace
