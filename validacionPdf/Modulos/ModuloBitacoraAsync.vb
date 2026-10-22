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
    Private _lotesEnEscritura As Integer = 0
    Private Const CadenaConexionDefault As String = "server=127.0.0.1;port=3307;user id=appuser;password=1234;database=stellumdb;ConnectionTimeout=15;DefaultCommandTimeout=60;Pooling=true;MinPoolSize=2;MaxPoolSize=60;"

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

    ''' <summary>
    ''' Espera a que todos los elementos en la cola y cualquier transacción en curso se confirmen en MySQL antes de cerrar.
    ''' </summary>
    Public Sub EsperarVaciado(Optional maxEsperaMs As Integer = 15000)
        Dim sw = Diagnostics.Stopwatch.StartNew()
        While (Not ColaBitacora.IsEmpty OrElse Interlocked.CompareExchange(_lotesEnEscritura, 0, 0) > 0) AndAlso sw.ElapsedMilliseconds < maxEsperaMs
            AutoReset.Set()
            Thread.Sleep(40)
        End While
    End Sub

    Private Sub ConsumidorLoop(token As CancellationToken)
        While Not token.IsCancellationRequested OrElse Not ColaBitacora.IsEmpty
            Try
                ' Esperar hasta 500ms por nuevos elementos
                AutoReset.WaitOne(500)

                If ColaBitacora.IsEmpty Then Continue While

                Interlocked.Increment(_lotesEnEscritura)
                Try
                    Dim loteItems As New List(Of ItemBitacora)()
                    Dim item As ItemBitacora = Nothing

                    ' Extraer hasta 50 elementos para inserción por lotes
                    While loteItems.Count < 50 AndAlso ColaBitacora.TryDequeue(item)
                        loteItems.Add(item)
                    End While

                    If loteItems.Count > 0 Then
                        InsertarLoteEnBaseDeDatos(loteItems)
                    End If
                Finally
                    Interlocked.Decrement(_lotesEnEscritura)
                End Try
            Catch ex As Exception
                Debug.WriteLine("[ModuloBitacoraAsync] Error en loop: " & ex.Message)
            End Try
        End While
    End Sub

    Private Sub AsegurarTablaTransferenciaTiff(conn As MySqlConnection)
        Dim sql = "CREATE TABLE IF NOT EXISTS bitacora_transferencia_tiff (" &
                  "id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY, " &
                  "disk VARCHAR(500) NULL, " &
                  "carpeta_origen VARCHAR(1000) NULL, " &
                  "carpeta_destino VARCHAR(1000) NULL, " &
                  "documento VARCHAR(1000) NULL, " &
                  "nombre_original VARCHAR(500) NULL, " &
                  "nombre_nuevo VARCHAR(500) NULL, " &
                  "error TEXT NULL, " &
                  "estatus VARCHAR(50) NULL, " &
                  "paginas INT NOT NULL DEFAULT 0, " &
                  "delegacion VARCHAR(255) NULL, " &
                  "usuario VARCHAR(255) NULL, " &
                  "anio VARCHAR(20) NULL, " &
                  "fecha DATETIME NULL, " &
                  "INDEX idx_doc (documento(190)), " &
                  "INDEX idx_orig (nombre_original(190)), " &
                  "INDEX idx_carpeta_orig (carpeta_origen(190)), " &
                  "INDEX idx_carpeta_dest (carpeta_destino(190))" &
                  ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"
        Using cmd As New MySqlCommand(sql, conn)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub InsertarLoteEnBaseDeDatos(items As List(Of ItemBitacora))
        Dim maxIntentos As Integer = 3
        Dim ultimoError As Exception = Nothing

        For intento As Integer = 1 To maxIntentos
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
                                Dim queryUpdateTiff As String = "
                                    UPDATE bitacora_transferencia_tiff SET
                                        estatus = @estatus,
                                        paginas = @paginas,
                                        error = @error,
                                        usuario = @usuario,
                                        fecha = NOW(),
                                        carpeta_destino = @carpeta_destino
                                    WHERE carpeta_destino = @carpeta_destino
                                       OR (carpeta_origen = @carpeta_origen AND (
                                           documento = @documento OR documento = @nombre_original OR documento = @nombre_nuevo
                                           OR nombre_original = @nombre_original OR nombre_original = @nombre_nuevo
                                       ))
                                "
                                Dim queryInsertTiff As String = "
                                    INSERT INTO bitacora_transferencia_tiff
                                    (disk, carpeta_origen, carpeta_destino, documento, nombre_original, nombre_nuevo, error, estatus, paginas, delegacion, usuario, anio, fecha)
                                    VALUES
                                    (@disk, @carpeta_origen, @carpeta_destino, @documento, @nombre_original, @nombre_nuevo, @error, @estatus, @paginas, @delegacion, @usuario, @anio, NOW())
                                "
                                Try
                                    Dim filasAct As Integer = 0
                                    Using cmdUpd As New MySqlCommand(queryUpdateTiff, conn, trans)
                                        cmdUpd.Parameters.AddWithValue("@estatus", item.Estatus)
                                        cmdUpd.Parameters.AddWithValue("@paginas", item.Paginas)
                                        cmdUpd.Parameters.AddWithValue("@error", item.ErrorMsg)
                                        cmdUpd.Parameters.AddWithValue("@usuario", item.Usuario)
                                        cmdUpd.Parameters.AddWithValue("@carpeta_destino", item.CarpetaDestino)
                                        cmdUpd.Parameters.AddWithValue("@carpeta_origen", item.CarpetaOrigen)
                                        cmdUpd.Parameters.AddWithValue("@documento", item.Documento)
                                        cmdUpd.Parameters.AddWithValue("@nombre_original", item.NombreOriginal)
                                        cmdUpd.Parameters.AddWithValue("@nombre_nuevo", item.NombreNuevo)
                                        filasAct = cmdUpd.ExecuteNonQuery()
                                    End Using

                                    If filasAct = 0 Then
                                        Using cmdIns As New MySqlCommand(queryInsertTiff, conn, trans)
                                            cmdIns.Parameters.AddWithValue("@disk", item.Disk)
                                            cmdIns.Parameters.AddWithValue("@carpeta_origen", item.CarpetaOrigen)
                                            cmdIns.Parameters.AddWithValue("@carpeta_destino", item.CarpetaDestino)
                                            cmdIns.Parameters.AddWithValue("@documento", item.Documento)
                                            cmdIns.Parameters.AddWithValue("@nombre_original", item.NombreOriginal)
                                            cmdIns.Parameters.AddWithValue("@nombre_nuevo", item.NombreNuevo)
                                            cmdIns.Parameters.AddWithValue("@error", item.ErrorMsg)
                                            cmdIns.Parameters.AddWithValue("@estatus", item.Estatus)
                                            cmdIns.Parameters.AddWithValue("@paginas", item.Paginas)
                                            cmdIns.Parameters.AddWithValue("@delegacion", item.Delegacion)
                                            cmdIns.Parameters.AddWithValue("@usuario", item.Usuario)
                                            cmdIns.Parameters.AddWithValue("@anio", If(String.IsNullOrEmpty(item.Anio), DBNull.Value, item.Anio))
                                            cmdIns.ExecuteNonQuery()
                                        End Using
                                    End If
                                Catch exTbl As MySqlException When exTbl.Number = 1146
                                    AsegurarTablaTransferenciaTiff(conn)
                                    Using cmdIns As New MySqlCommand(queryInsertTiff, conn, trans)
                                        cmdIns.Parameters.AddWithValue("@disk", item.Disk)
                                        cmdIns.Parameters.AddWithValue("@carpeta_origen", item.CarpetaOrigen)
                                        cmdIns.Parameters.AddWithValue("@carpeta_destino", item.CarpetaDestino)
                                        cmdIns.Parameters.AddWithValue("@documento", item.Documento)
                                        cmdIns.Parameters.AddWithValue("@nombre_original", item.NombreOriginal)
                                        cmdIns.Parameters.AddWithValue("@nombre_nuevo", item.NombreNuevo)
                                        cmdIns.Parameters.AddWithValue("@error", item.ErrorMsg)
                                        cmdIns.Parameters.AddWithValue("@estatus", item.Estatus)
                                        cmdIns.Parameters.AddWithValue("@paginas", item.Paginas)
                                        cmdIns.Parameters.AddWithValue("@delegacion", item.Delegacion)
                                        cmdIns.Parameters.AddWithValue("@usuario", item.Usuario)
                                        cmdIns.Parameters.AddWithValue("@anio", If(String.IsNullOrEmpty(item.Anio), DBNull.Value, item.Anio))
                                        cmdIns.ExecuteNonQuery()
                                    End Using
                                End Try
                            End If
                        Next

                        trans.Commit()
                    End Using
                End Using
                Exit Sub
            Catch ex As Exception
                ultimoError = ex
                If intento < maxIntentos Then
                    Thread.Sleep(400 * intento)
                End If
            End Try
        Next

        ' Fallback a log local solo si tras los 3 reintentos la base de datos sigue caída
        GuardarFallbackEnDisco(items, If(ultimoError IsNot Nothing, ultimoError.Message, "Error desconocido"))
    End Sub

    Private Sub GuardarFallbackEnDisco(items As List(Of ItemBitacora), errorDb As String)
        Try
            Dim rutaLog = Path.Combine(Application.StartupPath, "data", "bitacora_offline.log")
            Dim dirLog = Path.GetDirectoryName(rutaLog)
            If Not Directory.Exists(dirLog) Then Directory.CreateDirectory(dirLog)
            Dim lineas As New List(Of String)()
            For Each it In items
                lineas.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | [{it.Tipo}] | {it.Documento} | {it.Estatus} | Paginas: {it.Paginas} | DB_OFFLINE ({errorDb})")
            Next
            File.AppendAllLines(rutaLog, lineas)
        Catch
        End Try
    End Sub

End Module
