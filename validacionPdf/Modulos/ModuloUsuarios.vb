Imports Newtonsoft.Json
Imports System.IO

''' <summary>
''' Módulo para gestión de usuarios locales.
''' Los usuarios se almacenan en data/usuarios.json junto al ejecutable.
''' </summary>
Public Module ModuloUsuarios

    Public Class Usuario
        Public Property Id As Integer
        Public Property NombreCompleto As String
        Public Property NombreUsuario As String
        Public Property Pin As String
    End Class

    Private Class DatosUsuarios
        Public Property Usuarios As List(Of Usuario)
        Public Property PinMaestro As String
    End Class

    ''' <summary>
    ''' Usuario que tiene sesión activa actualmente.
    ''' </summary>
    Public UsuarioActual As Usuario = Nothing

    ''' <summary>
    ''' Identificador de la sesión actual (usuario_fecha_hora).
    ''' Se usa como nombre de la hoja en el Excel de auditoría.
    ''' </summary>
    Public SesionActual As String = ""

    Private ReadOnly CarpetaData As String = Path.Combine(Application.StartupPath, "data")
    Private ReadOnly ArchivoUsuarios As String = Path.Combine(Application.StartupPath, "data", "usuarios.json")

    ''' <summary>
    ''' Inicializa la carpeta de datos y el archivo de usuarios si no existen.
    ''' </summary>
    Public Sub InicializarDatos()
        If Not Directory.Exists(CarpetaData) Then
            Directory.CreateDirectory(CarpetaData)
        End If

        If Not File.Exists(ArchivoUsuarios) Then
            Dim datos As New DatosUsuarios With {
                .PinMaestro = "0000",
                .Usuarios = New List(Of Usuario) From {
                    New Usuario With {
                        .Id = 1,
                        .NombreCompleto = "Administrador",
                        .NombreUsuario = "admin",
                        .Pin = "0000"
                    }
                }
            }
            GuardarDatos(datos)
        End If
    End Sub

    ''' <summary>
    ''' Carga la lista de usuarios desde el archivo JSON.
    ''' </summary>
    Public Function CargarUsuarios() As List(Of Usuario)
        If Not File.Exists(ArchivoUsuarios) Then Return New List(Of Usuario)

        Dim json As String = File.ReadAllText(ArchivoUsuarios)
        Dim datos = JsonConvert.DeserializeObject(Of DatosUsuarios)(json)
        Return If(datos?.Usuarios, New List(Of Usuario))
    End Function

    ''' <summary>
    ''' Obtiene el PIN maestro para administración de usuarios.
    ''' </summary>
    Public Function ObtenerPinMaestro() As String
        If Not File.Exists(ArchivoUsuarios) Then Return "0000"

        Dim json As String = File.ReadAllText(ArchivoUsuarios)
        Dim datos = JsonConvert.DeserializeObject(Of DatosUsuarios)(json)
        Return If(datos?.PinMaestro, "0000")
    End Function

    ''' <summary>
    ''' Guarda la lista de usuarios en el archivo JSON.
    ''' </summary>
    Public Sub GuardarUsuarios(usuarios As List(Of Usuario))
        Dim datos As New DatosUsuarios With {
            .PinMaestro = ObtenerPinMaestro(),
            .Usuarios = usuarios
        }
        GuardarDatos(datos)
    End Sub

    ''' <summary>
    ''' Valida las credenciales de un usuario.
    ''' </summary>
    Public Function ValidarLogin(nombreUsuario As String, pin As String) As Usuario
        Dim usuarios = CargarUsuarios()
        Return usuarios.FirstOrDefault(Function(u) u.NombreUsuario.ToLower() = nombreUsuario.ToLower() AndAlso u.Pin = pin)
    End Function

    ''' <summary>
    ''' Agrega un nuevo usuario al catálogo.
    ''' </summary>
    Public Sub AgregarUsuario(nombreCompleto As String, nombreUsuario As String, pin As String)
        Dim usuarios = CargarUsuarios()
        Dim nuevoId = If(usuarios.Count > 0, usuarios.Max(Function(u) u.Id) + 1, 1)

        usuarios.Add(New Usuario With {
            .Id = nuevoId,
            .NombreCompleto = nombreCompleto,
            .NombreUsuario = nombreUsuario,
            .Pin = pin
        })

        GuardarUsuarios(usuarios)
    End Sub

    ''' <summary>
    ''' Elimina un usuario por su ID.
    ''' </summary>
    Public Sub EliminarUsuario(id As Integer)
        Dim usuarios = CargarUsuarios()
        usuarios.RemoveAll(Function(u) u.Id = id)
        GuardarUsuarios(usuarios)
    End Sub

    ''' <summary>
    ''' Inicia la sesión de un usuario y genera el identificador de sesión.
    ''' </summary>
    Public Sub IniciarSesion(usuario As Usuario)
        UsuarioActual = usuario
        SesionActual = usuario.NombreUsuario & "_" & DateTime.Now.ToString("yyyy-MM-dd_HH-mm")
    End Sub

    Private Sub GuardarDatos(datos As DatosUsuarios)
        Dim json As String = JsonConvert.SerializeObject(datos, Formatting.Indented)
        File.WriteAllText(ArchivoUsuarios, json)
    End Sub

End Module
