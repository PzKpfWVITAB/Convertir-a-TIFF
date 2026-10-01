Imports System.IO
Imports Newtonsoft.Json

Public Module ModuloDirectorios

    ' ===== Clases =====

    Public Class DirectorioInfo
        Public Property Id As String
        Public Property Nombre As String
        Public Property Ruta As String
        Public Property ReglaIds As List(Of String)
    End Class

    Public Class Delegacion
        Public Property Id As String
        Public Property NombreCarpeta As String   ' Sin acentos, igual que el nombre de carpeta en disco
        Public Property NombreCompleto As String
    End Class

    Public Class LoteInfo
        Public Property RutaCompleta As String
        Public Property NombreLote As String
        Public Property NombreDelegacion As String
        Public Property Delegacion As Delegacion
        Public Property Ocupado As Boolean
        Public Property OcupadoPor As String
    End Class

    Public Class LoteLock
        Public Property Usuario As String
        Public Property NombreCompleto As String
        Public Property PC As String
        Public Property FechaInicio As String
    End Class

    ' ===== Estado global de sesión de trabajo =====

    Public DirectorioActual As DirectorioInfo = Nothing
    Public DelegacionActual As Delegacion = Nothing
    Public LoteActual As String = ""
    Public RutaLoteActual As String = ""
    Public RutaEntregable As String = ""

    ' ===== Rutas de catálogos =====

    Private ReadOnly ArchivoDirectorios As String =
        Path.Combine(Application.StartupPath, "data", "directorios.json")
    Private ReadOnly ArchivoDelegaciones As String =
        Path.Combine(Application.StartupPath, "data", "delegaciones.json")

    ' ===== Cargar catálogos =====

    Public Function CargarDirectorios() As List(Of DirectorioInfo)
        Try
            If Not File.Exists(ArchivoDirectorios) Then InicializarDirectorios()
            Return JsonConvert.DeserializeObject(Of List(Of DirectorioInfo))(File.ReadAllText(ArchivoDirectorios))
        Catch ex As Exception
            Debug.WriteLine("Error al cargar directorios: " & ex.Message)
            Return New List(Of DirectorioInfo)()
        End Try
    End Function

    Public Function CargarDelegaciones() As List(Of Delegacion)
        Try
            If Not File.Exists(ArchivoDelegaciones) Then InicializarDelegaciones()
            Return JsonConvert.DeserializeObject(Of List(Of Delegacion))(File.ReadAllText(ArchivoDelegaciones))
        Catch ex As Exception
            Debug.WriteLine("Error al cargar delegaciones: " & ex.Message)
            Return New List(Of Delegacion)()
        End Try
    End Function

    Public Function BuscarDelegacionPorCarpeta(nombreCarpeta As String) As Delegacion
        Return CargarDelegaciones().FirstOrDefault(Function(d)
            Return String.Equals(d.NombreCarpeta, nombreCarpeta, StringComparison.OrdinalIgnoreCase)
        End Function)
    End Function

    ' ===== Conectividad =====

    Public Function DirectorioConectado(ruta As String) As Boolean
        Try
            Return Directory.Exists(ruta)
        Catch
            Return False
        End Try
    End Function

    ' ===== Escaneo de lotes en Respaldo_Original =====

    Public Function ObtenerLotes(dirInfo As DirectorioInfo) As List(Of LoteInfo)
        Dim resultado As New List(Of LoteInfo)()
        Dim rutaRespaldo = Path.Combine(dirInfo.Ruta, "Respaldo_Original")
        If Not Directory.Exists(rutaRespaldo) Then Return resultado

        For Each carpetaDel In Directory.GetDirectories(rutaRespaldo)
            Dim nombreDel = Path.GetFileName(carpetaDel)
            Dim delegacion = BuscarDelegacionPorCarpeta(nombreDel)

            For Each carpetaLote In Directory.GetDirectories(carpetaDel)
                Dim info As New LoteInfo With {
                    .RutaCompleta = carpetaLote,
                    .NombreLote = Path.GetFileName(carpetaLote),
                    .NombreDelegacion = nombreDel,
                    .Delegacion = delegacion
                }

                Dim archivoLock = Path.Combine(carpetaLote, "lote.lock")
                If File.Exists(archivoLock) Then
                    info.Ocupado = True
                    Try
                        Dim lockData = JsonConvert.DeserializeObject(Of LoteLock)(File.ReadAllText(archivoLock))
                        info.OcupadoPor = lockData.NombreCompleto & " [" & lockData.PC & "]"
                    Catch
                        info.OcupadoPor = "otro usuario"
                    End Try
                End If

                resultado.Add(info)
            Next
        Next

        Return resultado
    End Function

    ' ===== Búsqueda automática de lote =====

    Public Function BuscarLoteLibre(dirInfo As DirectorioInfo, delegacion As Delegacion) As LoteInfo
        Try
            Dim rutaDelegacion = Path.Combine(dirInfo.Ruta, "Respaldo_Original", delegacion.NombreCarpeta)
            
            If Not Directory.Exists(rutaDelegacion) Then Return Nothing

            ' Buscar solo las carpetas cuyo nombre inicie con "Lote"
            Dim carpetasLote = Directory.GetDirectories(rutaDelegacion).
                Where(Function(d) Path.GetFileName(d).ToLower().StartsWith("lote")).
                OrderBy(Function(d) d).ToList()

            For Each carpetaLote In carpetasLote
                Dim archivoLock = Path.Combine(carpetaLote, "lote.lock")
                
                ' Si no tiene lock, es un lote libre
                If Not File.Exists(archivoLock) Then
                    Return New LoteInfo With {
                        .RutaCompleta = carpetaLote,
                        .NombreLote = Path.GetFileName(carpetaLote),
                        .NombreDelegacion = delegacion.NombreCarpeta,
                        .Delegacion = delegacion,
                        .Ocupado = False
                    }
                End If
            Next

            Return Nothing ' Todos ocupados
        Catch ex As Exception
            Debug.WriteLine("Error al buscar lote libre: " & ex.Message)
            Return Nothing
        End Try
    End Function

    ' ===== Gestión de .lock =====

    Public Sub TomarLote(rutaLote As String)
        Try
            Dim lockData As New LoteLock With {
                .Usuario = ModuloUsuarios.UsuarioActual.NombreUsuario,
                .NombreCompleto = ModuloUsuarios.UsuarioActual.NombreCompleto,
                .PC = Environment.MachineName,
                .FechaInicio = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
            }
            File.WriteAllText(
                Path.Combine(rutaLote, "lote.lock"),
                JsonConvert.SerializeObject(lockData, Formatting.Indented))
        Catch ex As Exception
            MessageBox.Show("Error al crear bloqueo de lote: " & ex.Message)
        End Try
    End Sub

    Public Sub LiberarLoteActual()
        Try
            If String.IsNullOrEmpty(RutaLoteActual) Then Exit Sub
            Dim archivoLock = Path.Combine(RutaLoteActual, "lote.lock")
            If File.Exists(archivoLock) Then File.Delete(archivoLock)
        Catch ex As Exception
            Debug.WriteLine("Error al liberar lote: " & ex.Message)
        End Try
    End Sub

    ' ===== Inicializar ruta Entregable =====

    Public Sub InicializarRutaEntregable()
        If DirectorioActual Is Nothing OrElse DelegacionActual Is Nothing Then Exit Sub
        RutaEntregable = Path.Combine(DirectorioActual.Ruta, "Entregable", DelegacionActual.Id)
        If Not Directory.Exists(RutaEntregable) Then Directory.CreateDirectory(RutaEntregable)
    End Sub

    ' ===== Datos por defecto =====

    Private Sub InicializarDirectorios()
        Dim dirs As New List(Of DirectorioInfo) From {
            New DirectorioInfo With {
                .Id = "irec", .Nombre = "IREC",
                .Ruta = "\\172.40.5.84\irec",
                .ReglaIds = New List(Of String) From {"Regla1", "Regla2"}
            },
            New DirectorioInfo With {
                .Id = "cats", .Nombre = "CATS",
                .Ruta = "\\172.40.5.84\cats",
                .ReglaIds = New List(Of String) From {}
            },
            New DirectorioInfo With {
                .Id = "rciv", .Nombre = "RCIV",
                .Ruta = "\\172.40.5.84\rciv",
                .ReglaIds = New List(Of String) From {}
            }
        }
        File.WriteAllText(ArchivoDirectorios, JsonConvert.SerializeObject(dirs, Formatting.Indented))
    End Sub

    Private Sub InicializarDelegaciones()
        Dim dels As New List(Of Delegacion) From {
            New Delegacion With {.Id = "01", .NombreCarpeta = "Tuxtla Gutierrez",   .NombreCompleto = "TUXTLA GUTIÉRREZ"},
            New Delegacion With {.Id = "02", .NombreCarpeta = "San Cristobal",      .NombreCompleto = "SAN CRISTÓBAL"},
            New Delegacion With {.Id = "03", .NombreCarpeta = "Venustiano Carranza",.NombreCompleto = "VENUSTIANO CARRANZA"},
            New Delegacion With {.Id = "04", .NombreCarpeta = "Comitan",            .NombreCompleto = "COMITÁN"},
            New Delegacion With {.Id = "05", .NombreCarpeta = "Bochil",             .NombreCompleto = "BOCHIL"},
            New Delegacion With {.Id = "06", .NombreCarpeta = "Copainala",          .NombreCompleto = "COPAINALA"},
            New Delegacion With {.Id = "07", .NombreCarpeta = "Tonala",             .NombreCompleto = "TONALÁ"},
            New Delegacion With {.Id = "08", .NombreCarpeta = "Tapachula",          .NombreCompleto = "TAPACHULA"},
            New Delegacion With {.Id = "09", .NombreCarpeta = "Chiapa de Corzo",    .NombreCompleto = "CHIAPA DE CORZO"},
            New Delegacion With {.Id = "10", .NombreCarpeta = "Huixtla",            .NombreCompleto = "HUIXTLA"},
            New Delegacion With {.Id = "11", .NombreCarpeta = "Motozintla",         .NombreCompleto = "MOTOZINTLA"},
            New Delegacion With {.Id = "12", .NombreCarpeta = "Cintalapa",          .NombreCompleto = "CINTALAPA"},
            New Delegacion With {.Id = "13", .NombreCarpeta = "Acapetahua",         .NombreCompleto = "ACAPETAHUA"},
            New Delegacion With {.Id = "14", .NombreCarpeta = "Pichucalco",         .NombreCompleto = "PICHUCALCO"},
            New Delegacion With {.Id = "15", .NombreCarpeta = "Villaflores",        .NombreCompleto = "VILLAFLORES"},
            New Delegacion With {.Id = "16", .NombreCarpeta = "Salto de Agua",      .NombreCompleto = "SALTO DE AGUA"},
            New Delegacion With {.Id = "17", .NombreCarpeta = "Catazaja",           .NombreCompleto = "CATAZAJA"},
            New Delegacion With {.Id = "18", .NombreCarpeta = "Ocosingo",           .NombreCompleto = "OCOSINGO"},
            New Delegacion With {.Id = "19", .NombreCarpeta = "Yajalon",            .NombreCompleto = "YAJALÓN"}
        }
        File.WriteAllText(ArchivoDelegaciones, JsonConvert.SerializeObject(dels, Formatting.Indented))
    End Sub

End Module
