Imports Newtonsoft.Json
Imports System.IO

''' <summary>
''' Módulo para gestión del catálogo de años válidos.
''' Optimizado con caché en memoria RAM para evitar lecturas de disco por cada pulsación de tecla en la UI.
''' </summary>
Public Module ModuloCatalogo

    Private Class DatosAnios
        Public Property Anios As List(Of Integer)
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

    Private ReadOnly ArchivoAnios As String = Path.Combine(ObtenerCarpetaBase(), "data", "catalogo_anios.json")
    Private CacheAnios As HashSet(Of Integer) = Nothing
    Private ReadOnly LockCache As New Object()

    ''' <summary>
    ''' Inicializa el archivo de catálogo con años por defecto si no existe y pre-carga la caché en RAM.
    ''' </summary>
    Public Sub InicializarCatalogo()
        Dim carpeta = Path.GetDirectoryName(ArchivoAnios)
        If Not Directory.Exists(carpeta) Then Directory.CreateDirectory(carpeta)

        If Not File.Exists(ArchivoAnios) Then
            Dim anioActual = DateTime.Now.Year
            Dim datos As New DatosAnios With {
                .Anios = Enumerable.Range(anioActual - 5, 7).ToList()
            }
            Dim json As String = JsonConvert.SerializeObject(datos, Formatting.Indented)
            File.WriteAllText(ArchivoAnios, json)
        End If

        ' Cargar en memoria
        RecargarCache()
    End Sub

    Private Sub RecargarCache()
        SyncLock LockCache
            Dim lista = CargarAniosDesdeDisco()
            CacheAnios = New HashSet(Of Integer)(lista)
        End SyncLock
    End Sub

    Private Function CargarAniosDesdeDisco() As List(Of Integer)
        If Not File.Exists(ArchivoAnios) Then Return New List(Of Integer)
        Try
            Dim json As String = File.ReadAllText(ArchivoAnios)
            Dim datos = JsonConvert.DeserializeObject(Of DatosAnios)(json)
            Return If(datos?.Anios, New List(Of Integer))
        Catch ex As Exception
            Debug.WriteLine("Error al cargar catálogo de años: " & ex.Message)
            Return New List(Of Integer)
        End Try
    End Function

    ''' <summary>
    ''' Carga la lista de años válidos desde la caché o disco.
    ''' </summary>
    Public Function CargarAnios() As List(Of Integer)
        SyncLock LockCache
            If CacheAnios Is Nothing Then
                RecargarCache()
            End If
            Return CacheAnios.OrderBy(Function(a) a).ToList()
        End SyncLock
    End Function

    ''' <summary>
    ''' Guarda la lista de años válidos en el archivo JSON y actualiza la caché.
    ''' </summary>
    Public Sub GuardarAnios(anios As List(Of Integer))
        Dim datos As New DatosAnios With {
            .Anios = anios.OrderBy(Function(a) a).ToList()
        }
        Dim json As String = JsonConvert.SerializeObject(datos, Formatting.Indented)
        File.WriteAllText(ArchivoAnios, json)

        SyncLock LockCache
            CacheAnios = New HashSet(Of Integer)(datos.Anios)
        End SyncLock
    End Sub

    ''' <summary>
    ''' Agrega un año al catálogo si no existe.
    ''' </summary>
    Public Function AgregarAnio(anio As Integer) As Boolean
        Dim anios = CargarAnios()
        If anios.Contains(anio) Then Return False

        anios.Add(anio)
        GuardarAnios(anios)
        Return True
    End Function

    ''' <summary>
    ''' Elimina un año del catálogo.
    ''' </summary>
    Public Sub EliminarAnio(anio As Integer)
        Dim anios = CargarAnios()
        anios.Remove(anio)
        GuardarAnios(anios)
    End Sub

    ''' <summary>
    ''' Verifica si un año está en el catálogo de años válidos directamente desde la RAM (0 ms).
    ''' </summary>
    Public Function AnioEsValido(anio As Integer) As Boolean
        SyncLock LockCache
            If CacheAnios Is Nothing Then RecargarCache()
            Return CacheAnios.Contains(anio)
        End SyncLock
    End Function

End Module
