Imports System.Collections.Concurrent
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports MySqlConnector

''' <summary>
''' Módulo de registro asíncrono y desacoplado para bitácoras (MySQL y archivos de registro).
''' Emplea una cola concurrente (Producer-Consumer) para que los hilos de CPU no esperen por la red o por bloqueos (SyncLock).
''' </summary>
Public Module ModuloBitacoraAsync

    Public Enum TipoRegistroBitacora
        LoteGeneral
        TransferenciaTiff
    End Enum

    Public Class ItemBitacora
        Public Property Tipo As TipoRegistroBitacora
        Public Property Disk As String = ""
        Public Property CarpetaOrigen As String = ""
        Public Property CarpetaDestino As String = ""
        Public Property Documento As String = ""
        Public Property NombreOriginal As String = ""
        Public Property NombreNuevo As String = ""
        Public Property Comentarios As String = ""
        Public Property ErrorMsg As String = ""
        Public Property Estatus As String = ""
        Public Property TipoArchivo As String = ""
        Public Property Validado As Integer = 0
        Public Property Paginas As Integer = 0
        Public Property Delegacion As String = ""
        Public Property Usuario As String = ""
        Public Property Anio As String = ""
        Public Property Fecha As DateTime = DateTime.Now
    End Class

    Private ReadOnly ColaBitacora As New ConcurrentQueue(Of ItemBitacora)()
    Private ReadOnly AutoReset As New AutoResetEvent(False)
    Private CancelSource As CancellationTokenSource = Nothing
    Private TareaConsumidora As Task = Nothing
    Private Const CadenaConexionDefault As String = "server=127.0.0.1;port=3307;user id=appuser;password=1234;database=stellumdb;ConnectionTimeout=5;Pooling=true;MinPoolSize=2;MaxPoolSize=30;"

    Public Sub IniciarServicioBitacora()
        If TareaConsumidora IsNot Nothing AndAlso Not TareaConsumidora.IsCompleted Then Exit Sub

        CancelSource = New CancellationTokenSource()
        Dim token = CancelSource.Token
        TareaConsumidora = Task.Run(Sub() ConsumidorLoop(token), token)
    End Sub

    Public Sub EncolarLote(
        disk As String,
        carpetaOrigen As String,
        carpetaDestino As String,
        documento As String,
        nombreOriginal As String,
        nombreNuevo As String,
        comentarios As String,
        errorMsg As String,
        estatus As String,
        tipoArchivo As String,
        validado As Integer,
        paginas As Integer,
        usuario As String
    )
        IniciarServicioBitacora()

        Dim item As New ItemBitacora With {
            .Tipo = TipoRegistroBitacora.LoteGeneral,
            .Disk = disk,
            .CarpetaOrigen = carpetaOrigen,
            .CarpetaDestino = carpetaDestino,
            .Documento = documento,
            .NombreOriginal = nombreOriginal,
            .NombreNuevo = nombreNuevo,
            .Comentarios = comentarios,
            .ErrorMsg = errorMsg,
            .Estatus = estatus,
            .TipoArchivo = tipoArchivo,
            .Validado = validado,
            .Paginas = paginas,
            .Usuario = usuario
        }

        ColaBitacora.Enqueue(item)
        AutoReset.Set()
    End Sub

    Public Sub EncolarTransferenciaJpg(
        disk As String,
        carpetaOrigen As String,
        carpetaDestino As String,
        documento As String,
        nombreOriginal As String,
        nombreNuevo As String,
        errorMsg As String,
        estatus As String,
        paginas As Integer,
        delegacion As String,
        usuario As String,
        anio As String
    )
        IniciarServicioBitacora()

        Dim item As New ItemBitacora With {
            .Tipo = TipoRegistroBitacora.TransferenciaTiff,
            .Disk = disk,
            .CarpetaOrigen = carpetaOrigen,
            .CarpetaDestino = carpetaDestino,
            .Documento = documento,
            .NombreOriginal = nombreOriginal,
            .NombreNuevo = nombreNuevo,
            .ErrorMsg = errorMsg,
            .Estatus = estatus,
            .Paginas = paginas,
            .Delegacion = delegacion,
            .Usuario = usuario,
            .Anio = anio
        }

        ColaBitacora.Enqueue(item)
        AutoReset.Set()
    End Sub

    Public Sub EncolarTransferenciaTiff(
        disk As String,
        carpetaOrigen As String,
        carpetaDestino As String,
        documento As String,
        nombreOriginal As String,
        nombreNuevo As String,
        errorMsg As String,
        estatus As String,
        paginas As Integer,
        delegacion As String,
        usuario As String,
        anio As String
    )
        EncolarTransferenciaJpg(disk, carpetaOrigen, carpetaDestino, documento, nombreOriginal, nombreNuevo, errorMsg, estatus, paginas, delegacion, usuario, anio)
    End Sub

    ''' <summary>
    ''' Espera a que todos los elementos en la cola se inserten antes de cerrar.
    ''' </summary>
    Public Sub EsperarVaciado(Optional maxEsperaMs As Integer = 5000)
        Dim sw = Diagnostics.Stopwatch.StartNew()
        While Not ColaBitacora.IsEmpty AndAlso sw.ElapsedMilliseconds < maxEsperaMs
            AutoReset.Set()
            Thread.Sleep(50)
        End While
    End Sub

    Private Sub ConsumidorLoop(token As CancellationToken)
        While Not token.IsCancellationRequested OrElse Not ColaBitacora.IsEmpty
            Try
                ' Esperar hasta 500ms por nuevos elementos
                AutoReset.WaitOne(500)

                Dim loteItems As New List(Of ItemBitacora)()
                Dim item As ItemBitacora = Nothing

                ' Extraer hasta 50 elementos para inserción por lotes
                While loteItems.Count < 50 AndAlso ColaBitacora.TryDequeue(item)
                    loteItems.Add(item)
                End While

                If loteItems.Count > 0 Then
                    InsertarLoteEnBaseDeDatos(loteItems)
                End If
            Catch ex As Exception
                Debug.WriteLine("[ModuloBitacoraAsync] Error en loop: " & ex.Message)
            End Try
        End While
    End Sub

    Private Sub InsertarLoteEnBaseDeDatos(items As List(Of ItemBitacora))
        Try
            Using conn As New MySqlConnection(CadenaConexionDefault)
                conn.Open()
                Using trans = conn.BeginTransaction()

                    For Each item In items
                        If item.Tipo = TipoRegistroBitacora.LoteGeneral Then
                            Dim queryLote As String = "
                                INSERT INTO bitacora
                                (disk, carpeta_origen, carpeta_destino, documento, nombre_original, nombre_nuevo,
                                 comentarios, error, estatus, tipo_archivo, validado, paginas, usuario)
                                VALUES
                                (@disk, @origen, @destino, @doc, @nomOrig, @nomNuevo,
                                 @coment, @error, @estatus, @tipo, @validado, @paginas, @usuario)
                            "
                            Using cmd As New MySqlCommand(queryLote, conn, trans)
                                cmd.Parameters.AddWithValue("@disk", item.Disk)
                                cmd.Parameters.AddWithValue("@origen", item.CarpetaOrigen)
                                cmd.Parameters.AddWithValue("@destino", item.CarpetaDestino)
                                cmd.Parameters.AddWithValue("@doc", item.Documento)
                                cmd.Parameters.AddWithValue("@nomOrig", item.NombreOriginal)
                                cmd.Parameters.AddWithValue("@nomNuevo", item.NombreNuevo)
                                cmd.Parameters.AddWithValue("@coment", item.Comentarios)
                                cmd.Parameters.AddWithValue("@error", item.ErrorMsg)
                                cmd.Parameters.AddWithValue("@estatus", item.Estatus)
                                cmd.Parameters.AddWithValue("@tipo", item.TipoArchivo)
                                cmd.Parameters.AddWithValue("@validado", item.Validado)
                                cmd.Parameters.AddWithValue("@paginas", item.Paginas)
                                cmd.Parameters.AddWithValue("@usuario", item.Usuario)
                                cmd.ExecuteNonQuery()
                            End Using

                        ElseIf item.Tipo = TipoRegistroBitacora.TransferenciaTiff Then
                            Dim queryJpg As String = "
                                INSERT INTO bitacora_transferencia_jpg
                                (disk, carpeta_origen, carpeta_destino, documento, nombre_original, nombre_nuevo, error, estatus, paginas, delegacion, usuario, anio, fecha)
                                VALUES
                                (@disk, @carpeta_origen, @carpeta_destino, @documento, @nombre_original, @nombre_nuevo, @error, @estatus, @paginas, @delegacion, @usuario, @anio, NOW())
                            "
                            Try
                                Using cmd As New MySqlCommand(queryJpg, conn, trans)
                                    cmd.Parameters.AddWithValue("@disk", item.Disk)
                                    cmd.Parameters.AddWithValue("@carpeta_origen", item.CarpetaOrigen)
                                    cmd.Parameters.AddWithValue("@carpeta_destino", item.CarpetaDestino)
                                    cmd.Parameters.AddWithValue("@documento", item.Documento)
                                    cmd.Parameters.AddWithValue("@nombre_original", item.NombreOriginal)
                                    cmd.Parameters.AddWithValue("@nombre_nuevo", item.NombreNuevo)
                                    cmd.Parameters.AddWithValue("@error", item.ErrorMsg)
                                    cmd.Parameters.AddWithValue("@estatus", item.Estatus)
                                    cmd.Parameters.AddWithValue("@paginas", item.Paginas)
                                    cmd.Parameters.AddWithValue("@delegacion", item.Delegacion)
                                    cmd.Parameters.AddWithValue("@usuario", item.Usuario)
                                    cmd.Parameters.AddWithValue("@anio", If(String.IsNullOrEmpty(item.Anio), DBNull.Value, item.Anio))
                                    cmd.ExecuteNonQuery()
                                End Using
                            Catch exTbl As MySqlException When exTbl.Number = 1146
                                ' Fallback de compatibilidad si la tabla se llama bitacora_transferencia_tiff
                                Dim queryTiff As String = queryJpg.Replace("bitacora_transferencia_jpg", "bitacora_transferencia_tiff")
                                Using cmdFallback As New MySqlCommand(queryTiff, conn, trans)
                                    cmdFallback.Parameters.AddWithValue("@disk", item.Disk)
                                    cmdFallback.Parameters.AddWithValue("@carpeta_origen", item.CarpetaOrigen)
                                    cmdFallback.Parameters.AddWithValue("@carpeta_destino", item.CarpetaDestino)
                                    cmdFallback.Parameters.AddWithValue("@documento", item.Documento)
                                    cmdFallback.Parameters.AddWithValue("@nombre_original", item.NombreOriginal)
                                    cmdFallback.Parameters.AddWithValue("@nombre_nuevo", item.NombreNuevo)
                                    cmdFallback.Parameters.AddWithValue("@error", item.ErrorMsg)
                                    cmdFallback.Parameters.AddWithValue("@estatus", item.Estatus)
                                    cmdFallback.Parameters.AddWithValue("@paginas", item.Paginas)
                                    cmdFallback.Parameters.AddWithValue("@delegacion", item.Delegacion)
                                    cmdFallback.Parameters.AddWithValue("@usuario", item.Usuario)
                                    cmdFallback.Parameters.AddWithValue("@anio", If(String.IsNullOrEmpty(item.Anio), DBNull.Value, item.Anio))
                                    cmdFallback.ExecuteNonQuery()
                                End Using
                            End Try
                        End If
                    Next

                    trans.Commit()
                End Using
            End Using

        Catch ex As Exception
            ' Fallback a log local si la base de datos no está disponible para no perder los datos
            GuardarFallbackEnDisco(items, ex.Message)
        End Try
    End Sub

    Private Sub GuardarFallbackEnDisco(items As List(Of ItemBitacora), errorDb As String)
        Try
            Dim rutaLog = Path.Combine(Application.StartupPath, "data", "bitacora_offline.log")
            Dim lineas As New List(Of String)()
            For Each it In items
                lineas.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | [{it.Tipo}] | {it.Documento} | {it.Estatus} | Paginas: {it.Paginas} | DB_OFFLINE ({errorDb})")
            Next
            File.AppendAllLines(rutaLog, lineas)
        Catch
        End Try
    End Sub

End Module
