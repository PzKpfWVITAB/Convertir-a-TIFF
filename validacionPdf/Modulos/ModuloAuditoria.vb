Imports System.IO
Imports System.Threading.Tasks
Imports Newtonsoft.Json

''' <summary>
''' Auditoría local en formato JSON.
''' Registros organizados cronológicamente con campos de directorio, delegación y lote.
''' Optimizado para ejecutarse en segundo plano sin congelar la interfaz de usuario.
''' </summary>
Public Module ModuloAuditoria

    ' ===== Clase de registro =====

    Public Class RegistroAuditoria
        Public Property FechaHora As String
        Public Property Usuario As String
        Public Property NombreCompleto As String
        Public Property Directorio As String
        Public Property DelegacionId As String
        Public Property DelegacionNombre As String
        Public Property Lote As String
        Public Property Accion As String
        Public Property ArchivoOriginal As String
        Public Property ArchivoNuevo As String
        Public Property Detalles As String
    End Class

    Public Class ArchivoAuditoriaJson
        Public Property Registros As New List(Of RegistroAuditoria)
    End Class

    ' ===== Rutas =====

    Private ReadOnly ArchivoJson As String =
        Path.Combine(Application.StartupPath, "data", "auditoria.json")

    Private ReadOnly LockAuditoria As New Object()

    ' ===== Registrar acción =====

    Public Sub RegistrarAccion(accion As String,
                                archivoOriginal As String,
                                Optional archivoNuevo As String = "",
                                Optional detalles As String = "")

        If ModuloUsuarios.UsuarioActual Is Nothing Then Exit Sub

        Dim registro As New RegistroAuditoria With {
            .FechaHora       = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            .Usuario         = ModuloUsuarios.UsuarioActual.NombreUsuario,
            .NombreCompleto  = ModuloUsuarios.UsuarioActual.NombreCompleto,
            .Directorio      = If(ModuloDirectorios.DirectorioActual IsNot Nothing, ModuloDirectorios.DirectorioActual.Nombre, ""),
            .DelegacionId    = If(ModuloDirectorios.DelegacionActual IsNot Nothing, ModuloDirectorios.DelegacionActual.Id, ""),
            .DelegacionNombre = If(ModuloDirectorios.DelegacionActual IsNot Nothing, ModuloDirectorios.DelegacionActual.NombreCompleto, ""),
            .Lote            = ModuloDirectorios.LoteActual,
            .Accion          = accion,
            .ArchivoOriginal = archivoOriginal,
            .ArchivoNuevo    = archivoNuevo,
            .Detalles        = detalles
        }

        ' Guardar en segundo plano para no congelar la UI
        Task.Run(Sub() GuardarJson(registro))

    End Sub

    ' ===== Guardar en JSON =====

    Private Sub GuardarJson(registro As RegistroAuditoria)
        SyncLock LockAuditoria
            Try
                Dim datos As ArchivoAuditoriaJson

                If File.Exists(ArchivoJson) Then
                    datos = JsonConvert.DeserializeObject(Of ArchivoAuditoriaJson)(File.ReadAllText(ArchivoJson))
                    If datos Is Nothing Then datos = New ArchivoAuditoriaJson()
                Else
                    datos = New ArchivoAuditoriaJson()
                End If

                datos.Registros.Add(registro)
                File.WriteAllText(ArchivoJson, JsonConvert.SerializeObject(datos, Formatting.Indented))

            Catch ex As Exception
                Debug.WriteLine("Error JSON auditoria: " & ex.Message)
            End Try
        End SyncLock
    End Sub

End Module
