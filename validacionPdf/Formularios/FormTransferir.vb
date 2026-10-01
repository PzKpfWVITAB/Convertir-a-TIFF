Imports System.Collections.Concurrent
Imports System.ComponentModel
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Threading
Imports ImageMagick
Imports iText.Kernel.Pdf
Imports MySqlConnector
Imports Newtonsoft.Json

Public Class FormTransferir

    Dim totalPaginas As Integer = 0

    ' ORIGEN
    Private rutasOrigen As String() = {
        "\\172.40.5.84\irec",
        "\\172.40.5.84\cats",
        "\\172.40.5.84\rciv",
        "\\172.40.5.84\ssdirec",
        "\\172.40.5.84\ssdcats",
        "\\172.40.5.84\ssdrciv",
        "\\172.40.5.84\ssd002"
    }

    Private rutaOrigenActual As String = ""

    ' DESTINO
    Private rutasDestino As String() = {
        "\\172.40.5.84\irec",
        "\\172.40.5.84\cats",
        "\\172.40.5.84\rciv",
        "\\172.40.5.84\ssdirec",
        "\\172.40.5.84\ssdcats",
        "\\172.40.5.84\ssdrciv",
        "\\172.40.5.84\ssd002",
        "\\172.40.5.84\ssd003"
    }

    Private ReadOnly locker As New Object()
    Private rutaDestinoActual As String = ""
    Private Shared client As New HttpClient()

    ' Control de cancelación y atomicidad estricta (Congruencia 100% Disco <-> Base de Datos)
    Private cancelSourceTransfer As CancellationTokenSource = Nothing
    Private canceladoPorUsuario As Boolean = False
    Private ReadOnly ArchivosEnProceso As New ConcurrentDictionary(Of String, String)()

    Public Class TransferData
        Public Property Origen As String
        Public Property Destino As String
    End Class

    Private Sub FormTransferir_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ConfigurarGrid()

        ' Aplicar configuración dinámica de límites de hardware y búfer de staging
        ModuloRecursos.AplicarLimitesMagickNET()

        If Not client.DefaultRequestHeaders.Contains("X-API-KEY") Then
            client.DefaultRequestHeaders.Add("X-API-KEY", "MI_TOKEN_SECRETO")
        End If

        ConfigurarListViews()

        For Each ruta In rutasOrigen
            ListBoxOrigen.Items.Add(ruta)
        Next

        For Each ruta In rutasDestino
            ListBoxDestino.Items.Add(ruta)
        Next
    End Sub

    Private Sub FormTransferir_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If bgWorker IsNot Nothing AndAlso bgWorker.IsBusy Then
            Dim resp = MessageBox.Show(
                "Hay un proceso de transferencia en ejecución." & vbCrLf &
                "¿Desea cancelar el proceso y salir?" & vbCrLf & vbCrLf &
                "⚠️ Todos los archivos que se encuentren a la mitad serán eliminados automáticamente para asegurar congruencia total con la base de datos.",
                "Confirmar Salida y Cancelación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning)

            If resp = DialogResult.Yes Then
                CancelarProcesoYLimpiar()
                ' Esperar brevemente a que los hilos limpien las carpetas incompletas
                Dim sw = Diagnostics.Stopwatch.StartNew()
                While bgWorker.IsBusy AndAlso sw.ElapsedMilliseconds < 3500
                    Application.DoEvents()
                    Thread.Sleep(100)
                End While
            Else
                e.Cancel = True
            End If
        End If
    End Sub

    Private Sub btnConfigRecursos_Click(sender As Object, e As EventArgs) Handles btnConfigRecursos.Click
        Dim frm As New FormConfigRecursos()
        If frm.ShowDialog(Me) = DialogResult.OK Then
            ModuloRecursos.AplicarLimitesMagickNET()
        End If
    End Sub

    Private Sub ConfigurarListViews()
        With ListViewOrigen
            .View = View.Details
            .FullRowSelect = True
            .Columns.Add("Nombre", 300)
            .Columns.Add("Tipo", 100)
        End With

        With ListViewDestino
            .View = View.Details
            .FullRowSelect = True
            .Columns.Add("Nombre", 300)
            .Columns.Add("Tipo", 100)
        End With
    End Sub

    Private Sub CargarCarpetas(ruta As String, listView As ListView)
        listView.Items.Clear()
        Try
            If IO.Directory.GetParent(ruta) IsNot Nothing Then
                InteropUpItem(listView)
            End If

            For Each carpeta In IO.Directory.GetDirectories(ruta)
                Dim item As New ListViewItem(IO.Path.GetFileName(carpeta))
                item.SubItems.Add("Carpeta")
                listView.Items.Add(item)
            Next
        Catch ex As Exception
            MessageBox.Show("Error accediendo a: " & ruta & vbCrLf & ex.Message)
        End Try
    End Sub

    Private Sub InteropUpItem(listView As ListView)
        Dim itemUp As New ListViewItem("..")
        itemUp.SubItems.Add("Subir")
        listView.Items.Add(itemUp)
    End Sub

    Private Sub ListBoxOrigen_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBoxOrigen.SelectedIndexChanged
        If ListBoxOrigen.SelectedItem Is Nothing Then Exit Sub
        rutaOrigenActual = ListBoxOrigen.SelectedItem.ToString()
        LabelOrigenRuta.Text = rutaOrigenActual
        CargarCarpetas(rutaOrigenActual, ListViewOrigen)
    End Sub

    Private Sub ListBoxDestino_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBoxDestino.SelectedIndexChanged
        If ListBoxDestino.SelectedItem Is Nothing Then Exit Sub
        rutaDestinoActual = ListBoxDestino.SelectedItem.ToString()
        LabelDestinoRuta.Text = rutaDestinoActual
        CargarCarpetas(rutaDestinoActual, ListViewDestino)
    End Sub

    Private Sub ListViewOrigen_DoubleClick(sender As Object, e As EventArgs) Handles ListViewOrigen.DoubleClick
        If ListViewOrigen.SelectedItems.Count = 0 Then Exit Sub
        Dim nombre = ListViewOrigen.SelectedItems(0).Text
        If nombre = ".." Then
            Dim parent = IO.Directory.GetParent(rutaOrigenActual)
            If parent IsNot Nothing Then rutaOrigenActual = parent.FullName
        Else
            Dim nueva = IO.Path.Combine(rutaOrigenActual, nombre)
            If IO.Directory.Exists(nueva) Then rutaOrigenActual = nueva
        End If
        LabelOrigenRuta.Text = rutaOrigenActual
        CargarCarpetas(rutaOrigenActual, ListViewOrigen)
    End Sub

    Private Sub ListViewDestino_DoubleClick(sender As Object, e As EventArgs) Handles ListViewDestino.DoubleClick
        If ListViewDestino.SelectedItems.Count = 0 Then Exit Sub
        Dim nombre = ListViewDestino.SelectedItems(0).Text
        If nombre = ".." Then
            Dim parent = IO.Directory.GetParent(rutaDestinoActual)
            If parent IsNot Nothing Then rutaDestinoActual = parent.FullName
        Else
            Dim nueva = IO.Path.Combine(rutaDestinoActual, nombre)
            If IO.Directory.Exists(nueva) Then rutaDestinoActual = nueva
        End If
        LabelDestinoRuta.Text = rutaDestinoActual
        CargarCarpetas(rutaDestinoActual, ListViewDestino)
    End Sub

    Private Sub btnTransferir_Click(sender As Object, e As EventArgs) Handles btnTransferir.Click
        If String.IsNullOrEmpty(rutaOrigenActual) OrElse String.IsNullOrEmpty(rutaDestinoActual) Then
            MessageBox.Show("Selecciona origen y destino")
            Exit Sub
        End If

        If Not bgWorker.IsBusy Then
            canceladoPorUsuario = False
            cancelSourceTransfer = New CancellationTokenSource()
            ArchivosEnProceso.Clear()

            btnTransferir.Enabled = False
            btnCancelar.Enabled = True
            btnCancelar.Text = "Cancelar"

            Dim data As New TransferData With {
                .Origen = rutaOrigenActual,
                .Destino = rutaDestinoActual
            }
            dgvBitacora.Rows.Clear()
            bgWorker.RunWorkerAsync(data)
            Dim hilosAsignados = ModuloRecursos.ObtenerHilosOptimos()
            EscribirLog($"INICIO | Transferencia iniciada con {hilosAsignados} hilos paralelos dinámicos.")
        Else
            MessageBox.Show("Ya hay un proceso en ejecución")
        End If
    End Sub

    Private Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click
        If bgWorker.IsBusy Then
            Dim resp = MessageBox.Show(
                "¿Desea cancelar el proceso de transferencia?" & vbCrLf & vbCrLf &
                "⚠️ Los archivos que se encuentren a la mitad se eliminarán de inmediato y no se registrarán en la base de datos.",
                "Confirmar Cancelación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If resp = DialogResult.Yes Then
                btnCancelar.Enabled = False
                btnCancelar.Text = "Cancelando..."
                CancelarProcesoYLimpiar()
            End If
        End If
    End Sub

    Private Sub CancelarProcesoYLimpiar()
        canceladoPorUsuario = True
        If cancelSourceTransfer IsNot Nothing AndAlso Not cancelSourceTransfer.IsCancellationRequested Then
            cancelSourceTransfer.Cancel()
        End If
        If bgWorker IsNot Nothing AndAlso bgWorker.IsBusy Then
            bgWorker.CancelAsync()
        End If
        EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | CANCELACIÓN SOLICITADA POR EL USUARIO")
    End Sub

    Private Sub bgWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles bgWorker.DoWork
        Dim data As TransferData = CType(e.Argument, TransferData)
        FieldMappingAndConversion(data.Origen, data.Destino)
        If canceladoPorUsuario Then e.Cancel = True
    End Sub

    Private Sub bgWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles bgWorker.RunWorkerCompleted
        btnTransferir.Enabled = True
        btnCancelar.Enabled = False
        btnCancelar.Text = "Cancelar"

        If canceladoPorUsuario OrElse e.Cancelled Then
            dgvBitacora.Rows.Add("PROCESO CANCELADO", "Incompletos limpiados de disco")
            MessageBox.Show("El proceso fue cancelado. Se eliminaron todos los archivos que estaban a la mitad y la base de datos permanece 100% congruente con los archivos que sí se completaron.", "Cancelación Completada", MessageBoxButtons.OK, MessageBoxIcon.Information)
        ElseIf e.Error IsNot Nothing Then
            MessageBox.Show("Ocurrió un error en la transferencia: " & e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Else
            MessageBox.Show("Transferencia finalizada exitosamente.", "Proceso Terminado", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub FieldMappingAndConversion(origen As String, destino As String)
        ' Asegurar servicio de bitácora asíncrona
        ModuloBitacoraAsync.IniciarServicioBitacora()

        Dim dirInfo As New DirectoryInfo(origen)
        Dim archivos As FileInfo() = dirInfo.EnumerateFiles("*.pdf", SearchOption.AllDirectories).ToArray()
        Dim total As Integer = archivos.Length
        Dim procesados As Integer = 0
        Dim delegacionSeleccionada As String = New DirectoryInfo(destino).Name

        If total = 0 Then
            EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | No se encontraron archivos PDF en origen: {origen}")
            Exit Sub
        End If

        ' Obtener dinámicamente hilos óptimos (con 10% libre de reserva)
        Dim maxHilos As Integer = ModuloRecursos.ObtenerHilosOptimos()
        Dim cancelToken As CancellationToken = If(cancelSourceTransfer IsNot Nothing, cancelSourceTransfer.Token, CancellationToken.None)

        ' =========================================================================
        ' FASE DE PRE-VERIFICACIÓN: Detectar PDFs ya procesados completos en destino
        ' =========================================================================
        If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
            bgWorker.ReportProgress(0, "STATUS|Iniciando pre-verificación en destino...")
        End If

        Dim bagPendientes As New ConcurrentBag(Of FileInfo)()
        Dim bagOmitidos As New ConcurrentBag(Of KeyValuePair(Of String, Integer))()
        Dim contadorChequeados As Integer = 0

        Parallel.ForEach(archivos, Sub(archivoInfo, state)
                                       If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
                                           state.Stop()
                                           Exit Sub
                                       End If

                                       Dim pagsCompletas As Integer = 0
                                       If EstaPdfYaProcesadoYCompleto(origen, destino, archivoInfo.FullName, pagsCompletas) Then
                                           bagOmitidos.Add(New KeyValuePair(Of String, Integer)(archivoInfo.FullName, pagsCompletas))
                                       Else
                                           bagPendientes.Add(archivoInfo)
                                       End If

                                       Dim actual = Interlocked.Increment(contadorChequeados)
                                       If (actual Mod 100 = 0 OrElse actual = total) AndAlso bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                                           Dim pct = CInt((actual / Math.Max(1, total)) * 10)
                                           bgWorker.ReportProgress(pct, $"STATUS|Pre-chequeo: {actual:#,##0}/{total:#,##0} analizados ({bagOmitidos.Count:#,##0} ya listos)")
                                       End If
                                   End Sub)

        If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
            Exit Sub
        End If

        ' ORDENAR ESTRICTAMENTE DESCENDENTE POR PESO ORIGINAL DEL PDF (MÁS PESADOS PRIMERO, MENOS PESADOS AL FINAL)
        Dim listaPendientes = bagPendientes.OrderByDescending(Function(f) f.Length).ToList()
        Dim listaOmitidos = bagOmitidos.ToList()
        Dim totalOmitidos = listaOmitidos.Count
        Dim totalPendientes = listaPendientes.Count

        Dim pesoMaxMB = If(totalPendientes > 0, (listaPendientes.First().Length / (1024.0 * 1024.0)), 0)
        Dim pesoMinMB = If(totalPendientes > 0, (listaPendientes.Last().Length / (1024.0 * 1024.0)), 0)

        EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | PRE-CHEQUEO FINALIZADO | Total: {total} | Ya completos (omitidos): {totalOmitidos} | Pendientes por procesar: {totalPendientes} (Rango peso: {pesoMaxMB:0.1} MB máx -> {pesoMinMB:0.1} MB mín)")

        ' Reportar en cuadrícula de forma limpia sin saturar la interfaz
        If totalOmitidos > 0 Then
            If totalOmitidos <= 30 Then
                For Each item In listaOmitidos
                    Dim nomOmitido = Path.GetFileNameWithoutExtension(item.Key)
                    If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                        bgWorker.ReportProgress(10, $"{nomOmitido} | Ya procesado (Omitido: {item.Value} págs)")
                    End If
                Next
            Else
                If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                    bgWorker.ReportProgress(10, $"PRE-CHEQUEO DESTINO | {totalOmitidos:#,##0} archivos ya completos en destino (Omitidos)")
                End If
            End If
        End If

        ' Si todos los archivos ya están completos, terminar inmediatamente
        If totalPendientes = 0 Then
            If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                bgWorker.ReportProgress(100, $"Todos los archivos ({total:#,##0}) ya están completos en destino | Finalizado sin pendientes")
            End If
            EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | FIN | Todos los {total} archivos ya estaban completos en destino. Ninguno requirió conversión.")
            Exit Sub
        End If

        If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
            bgWorker.ReportProgress(10, $"COMIENZO DE CONVERSIÓN | {totalPendientes:#,##0} pendientes (Ordenados por peso: {pesoMaxMB:0.1} MB máx -> {pesoMinMB:0.1} MB mín)")
        End If

        ' Segmentación dual por peso:
        ' 1. PDFs Grandes (> 30 MB): Procesados secuencialmente a nivel de documento,
        '    pero utilizando los 100 hilos concurrentes para sus páginas (Protección total de disco local).
        ' 2. PDFs Pequeños (<= 30 MB): Procesados a nivel de documento con 100 hilos concurrentes.
        Dim limiteGrandeBytes As Long = ModuloRecursos.LimitePdfGrandeBytes
        Dim listaGrandes = listaPendientes.Where(Function(f) f.Length > limiteGrandeBytes).ToList()
        Dim listaPequenos = listaPendientes.Where(Function(f) f.Length <= limiteGrandeBytes).ToList()

        EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | DISTRIBUCIÓN DINÁMICA | Total: {totalPendientes} | Grandes (> 30 MB): {listaGrandes.Count} | Pequeños (<= 30 MB): {listaPequenos.Count}")

        ' =========================================================================
        ' FASE 1: PDFs GRANDES (> 30 MB) — 1 DOCUMENTO A LA VEZ CON 100 HILOS EN SUS PÁGINAS
        ' =========================================================================
        If listaGrandes.Count > 0 Then
            If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                bgWorker.ReportProgress(10, $"FASE 1: PDFs GRANDES | {listaGrandes.Count} archivos (> 30 MB) con {maxHilos} hilos por página (Protección de disco)")
            End If

            For Each fileInfo In listaGrandes
                If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then Exit For

                Dim paginas As Integer = 0
                Dim pesoMB As Double = fileInfo.Length / (1024.0 * 1024.0)
                Dim nombreSinExt = Path.GetFileNameWithoutExtension(fileInfo.FullName)

                If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                    Dim pctActual = CInt(10 + ((procesados / Math.Max(1, totalPendientes)) * 90))
                    bgWorker.ReportProgress(pctActual, $"STATUS|Procesando PDF Grande con {maxHilos} hilos: {nombreSinExt} ({pesoMB:0.1} MB)")
                End If

                Dim estatus = ProcesarUnArchivoPdf(fileInfo, origen, destino, delegacionSeleccionada, cancelToken, True, maxHilos, paginas)
                Dim actualProcesados = Interlocked.Increment(procesados)
                Dim porcentaje = CInt(10 + ((actualProcesados / Math.Max(1, totalPendientes)) * 90))

                If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress AndAlso Not canceladoPorUsuario Then
                    bgWorker.ReportProgress(porcentaje, $"{nombreSinExt} | {estatus} ({pesoMB:0.1} MB - {paginas} págs [100 hilos] - {actualProcesados}/{totalPendientes})")
                End If
            Next
        End If

        ' =========================================================================
        ' FASE 2: PDFs PEQUEÑOS (<= 30 MB) — 100 HILOS CONCURRENTES A NIVEL DE ARCHIVO
        ' =========================================================================
        If listaPequenos.Count > 0 AndAlso Not canceladoPorUsuario AndAlso Not cancelToken.IsCancellationRequested Then
            If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                Dim pctActual = CInt(10 + ((procesados / Math.Max(1, totalPendientes)) * 90))
                bgWorker.ReportProgress(pctActual, $"FASE 2: PDFs PEQUEÑOS | {listaPequenos.Count} archivos (<= 30 MB) a {maxHilos} hilos concurrentes")
            End If

            Dim options As New ParallelOptions With {
                .MaxDegreeOfParallelism = maxHilos,
                .CancellationToken = cancelToken
            }

            Try
                Dim particionador = Partitioner.Create(listaPequenos, EnumerablePartitionerOptions.NoBuffering)
                Parallel.ForEach(particionador, options, Sub(fileInfo, loopState, loopIndex)
                    If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
                        loopState.Stop()
                        Exit Sub
                    End If

                    Dim paginas As Integer = 0
                    Dim pesoMB As Double = fileInfo.Length / (1024.0 * 1024.0)
                    Dim nombreSinExt = Path.GetFileNameWithoutExtension(fileInfo.FullName)

                    Dim estatus = ProcesarUnArchivoPdf(fileInfo, origen, destino, delegacionSeleccionada, cancelToken, False, 1, paginas)
                    Dim actualProcesados = Interlocked.Increment(procesados)
                    Dim porcentaje = CInt(10 + ((actualProcesados / Math.Max(1, totalPendientes)) * 90))

                    If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress AndAlso Not canceladoPorUsuario Then
                        bgWorker.ReportProgress(porcentaje, $"{nombreSinExt} | {estatus} ({pesoMB:0.1} MB - {paginas} págs - {actualProcesados}/{totalPendientes})")
                    End If
                End Sub)
            Catch ex As OperationCanceledException
                ' Manejo normal de cancelación del bucle paralelo
            End Try
        End If

        ' Si hubo cancelación, barrer y asegurar limpieza de cualquier archivo que estuviera en vuelo
        If canceladoPorUsuario Then
            LimpiarTodosLosIncompletosEnVuelo()
        End If

        ' Esperar brevemente que la cola de bitácora termine de insertar solo lo completado
        ModuloBitacoraAsync.EsperarVaciado(2500)
        EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | FIN PROCESO | Origen: {origen} | Cancelado: {canceladoPorUsuario}")
    End Sub

    ''' <summary>
    ''' Procesa un único archivo PDF: gestiona rutas relativas, llamadas de conversión, verificación física,
    ''' registro en base de datos y bitácora, y limpieza inmediata de carpetas incompletas en caso de fallo.
    ''' </summary>
    Private Function ProcesarUnArchivoPdf(
        fileInfo As FileInfo,
        origen As String,
        destino As String,
        delegacionSeleccionada As String,
        cancelToken As CancellationToken,
        procesarPaginasEnParalelo As Boolean,
        hilosParalelos As Integer,
        ByRef totalPaginas As Integer
    ) As String
        Dim archivo As String = fileInfo.FullName
        Dim nombreSinExt As String = Path.GetFileNameWithoutExtension(archivo)
        Dim estatusFinal As String = "ERROR"
        Dim paginas As Integer = 0
        Dim carpetaPdfDestino As String = ""
        Dim archivoCompletadoConExito As Boolean = False
        Dim pesoMB As Double = fileInfo.Length / (1024.0 * 1024.0)

        Try
            Dim rutaRelativaArchivo As String = ObtenerRutaRelativa(origen, archivo)
            Dim directorioRelativo As String = Path.GetDirectoryName(rutaRelativaArchivo)
            Dim destinoDirectorioFinal As String = Path.Combine(destino, directorioRelativo)

            carpetaPdfDestino = Path.Combine(destinoDirectorioFinal, nombreSinExt)

            ' Registrar archivo en progreso para limpieza en caso de corte abrupto
            ArchivosEnProceso.TryAdd(archivo, carpetaPdfDestino)

            Dim anioEncontrado As String = Nothing
            Dim matchAnio = System.Text.RegularExpressions.Regex.Match(nombreSinExt, "\b(19\d{2}|20\d{2})\b")
            If matchAnio.Success Then
                anioEncontrado = matchAnio.Value
            End If

            ' Conversión de alto rendimiento con validación de cancelación paso a paso
            ConvertirPdfTiffOptimizado(archivo, carpetaPdfDestino, nombreSinExt, paginas, cancelToken, procesarPaginasEnParalelo, hilosParalelos)
            totalPaginas = paginas

            ' Verificar que no se haya cancelado durante la escritura
            If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
                LimpiarCarpetaIncompleta(carpetaPdfDestino)
                Return "CANCELADO"
            End If

            ' VERIFICACIÓN FÍSICA RÁPIDA DE INTEGRIDAD Y NÚMERO DE PÁGINAS
            Dim errorVerificacion As String = ""
            Dim esValidoFisicamente = ValidarIntegridadFisicaTiff(carpetaPdfDestino, nombreSinExt, paginas, errorVerificacion)

            If esValidoFisicamente AndAlso Not canceladoPorUsuario AndAlso Not cancelToken.IsCancellationRequested Then
                estatusFinal = "OK"
                archivoCompletadoConExito = True
            Else
                estatusFinal = If(Not String.IsNullOrEmpty(errorVerificacion), "ERROR: " & errorVerificacion, "ERROR: Verificación física fallida")
                archivoCompletadoConExito = False
                LimpiarCarpetaIncompleta(carpetaPdfDestino)
                EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | VERIFICACIÓN FALLIDA | {nombreSinExt} | {estatusFinal}")
            End If

            ' SÓLO SI EL ARCHIVO ESTÁ FÍSICAMENTE COMPLETO, VERIFICADO Y NO HUBO CANCELACIÓN SE REGISTRA EN BD
            If archivoCompletadoConExito AndAlso Not canceladoPorUsuario Then
                Dim usuarioActual = If(ModuloUsuarios.UsuarioActual IsNot Nothing, ModuloUsuarios.UsuarioActual.NombreUsuario, Environment.UserName)
                ModuloBitacoraAsync.EncolarTransferenciaTiff(
                    origen,
                    Path.GetDirectoryName(archivo),
                    carpetaPdfDestino,
                    archivo,
                    Path.GetFileName(archivo),
                    nombreSinExt,
                    "",
                    estatusFinal,
                    paginas,
                    delegacionSeleccionada,
                    usuarioActual,
                    anioEncontrado
                )

                Dim modoHilosStr = If(procesarPaginasEnParalelo, $" [{hilosParalelos} hilos]", "")
                EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {estatusFinal} | {nombreSinExt} | {pesoMB:0.1} MB ({paginas} páginas){modoHilosStr}")
            End If

        Catch ex As OperationCanceledException
            LimpiarCarpetaIncompleta(carpetaPdfDestino)
            estatusFinal = "CANCELADO"
        Catch ex As Exception
            estatusFinal = "ERROR"
            LimpiarCarpetaIncompleta(carpetaPdfDestino)
            Dim detalleError As String = ex.Message
            If TypeOf ex Is AggregateException Then
                Dim agg = DirectCast(ex, AggregateException).Flatten()
                detalleError = String.Join(" | ", agg.InnerExceptions.Select(Function(ie) ie.GetType().Name & ": " & ie.Message))
            ElseIf ex.InnerException IsNot Nothing Then
                detalleError &= " -> " & ex.InnerException.GetType().Name & ": " & ex.InnerException.Message
            End If
            EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ERROR | {nombreSinExt} | {detalleError}")
        Finally
            Dim dummy As String = Nothing
            ArchivosEnProceso.TryRemove(archivo, dummy)
        End Try

        Return estatusFinal
    End Function

    ''' <summary>
    ''' Conversión atómica a TIFF con control de cancelación y guardián de espacio en disco:
    ''' Si ocurre una cancelación o error en cualquier momento, el destino se limpia y no deja rastros.
    ''' Soporta paralelismo a nivel de página (para PDFs grandes) y secuencial (para PDFs pequeños).
    ''' </summary>
    Private Sub ConvertirPdfTiffOptimizado(
        pdfPath As String,
        carpetaDestino As String,
        nombreBase As String,
        ByRef totalPaginas As Integer,
        token As CancellationToken,
        Optional procesarPaginasEnParalelo As Boolean = False,
        Optional hilosParalelos As Integer = 1
    )
        If token.IsCancellationRequested Then Throw New OperationCanceledException()

        If Not File.Exists(pdfPath) Then
            Throw New FileNotFoundException("No se encontró el archivo PDF: " & pdfPath)
        End If

        ' Guardián de almacenamiento: Esperar disponibilidad de espacio si el disco local está en niveles críticos (< 10 GB)
        ModuloRecursos.EsperarEspacioDisponible(10.0, token, 30000)

        Dim rutaStagingBase = ModuloRecursos.ObtenerRutaStaging()
        Dim stagingCarpeta = Path.Combine(rutaStagingBase, "stg_" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(stagingCarpeta)

        Try
            If token.IsCancellationRequested Then Throw New OperationCanceledException()

            ' Determinar ruta de lectura del PDF:
            ' Si hay espacio de sobra en disco (> peso del PDF + 15 GB), copiamos a staging local para lectura NVMe ultrarrápida.
            ' De lo contrario, leemos directamente del origen sin riesgo de saturar el disco.
            Dim rutaPdfToRead As String = pdfPath
            Dim pdfLocal = Path.Combine(stagingCarpeta, "documento.pdf")
            Dim pesoPdfGB = (New FileInfo(pdfPath).Length / (1024.0 * 1024.0 * 1024.0))

            If ModuloRecursos.ObtenerEspacioLibreDiscoGB(rutaStagingBase) > (pesoPdfGB + 15.0) Then
                Try
                    File.Copy(pdfPath, pdfLocal, True)
                    rutaPdfToRead = pdfLocal
                Catch
                    rutaPdfToRead = pdfPath
                End Try
            End If

            If token.IsCancellationRequested Then Throw New OperationCanceledException()

            Dim settings As New MagickReadSettings()
            settings.Density = New Density(250, 250)

            ' Lectura lineal en una sola pasada continua (máxima velocidad de Ghostscript sin re-aperturas O(N^2))
            Using collection As New MagickImageCollection()
                collection.Read(rutaPdfToRead, settings)
                totalPaginas = collection.Count

                If totalPaginas <= 0 Then
                    Throw New InvalidDataException("El archivo PDF no contiene páginas válidas o está corrupto: " & pdfPath)
                End If

                ' AHORRO PROACTIVO DE DISCO: Eliminar el PDF copiado local ya que sus páginas residen en memoria
                Try
                    If File.Exists(pdfLocal) Then File.Delete(pdfLocal)
                Catch
                End Try

                Dim numTotalPaginas = totalPaginas
                Dim pesoMB = (New FileInfo(pdfPath).Length / (1024.0 * 1024.0))

                If procesarPaginasEnParalelo AndAlso hilosParalelos > 1 Then
                    ' PARALELISMO DE CPU (100 núcleos para cuantizar, comprimir a LZW y escribir TIFFs en memoria/disco)
                    Dim optionsPaginas As New ParallelOptions With {
                        .MaxDegreeOfParallelism = hilosParalelos,
                        .CancellationToken = token
                    }

                    Dim contadorPaginasProcesadas As Integer = 0
                    Parallel.For(0, collection.Count, optionsPaginas, Sub(i)
                        If token.IsCancellationRequested Then Throw New OperationCanceledException()

                        Dim page = collection(i)
                        page.Format = MagickFormat.Tiff
                        page.Settings.Compression = CompressionMethod.LZW
                        page.Alpha(AlphaOption.Remove)

                        Try
                            page.Quantize(New QuantizeSettings() With {
                                .Colors = 2,
                                .ColorSpace = ColorSpace.Gray
                            })
                        Catch
                            page.ColorSpace = ColorSpace.Gray
                        End Try

                        Dim tiffLocal = Path.Combine(stagingCarpeta, $"{nombreBase}_{i + 1}.tiff")
                        page.Write(tiffLocal)

                        Dim fiLocal As New FileInfo(tiffLocal)
                        If Not fiLocal.Exists OrElse fiLocal.Length < 16 Then
                            Throw New IOException($"Fallo al generar la página {i + 1} de {numTotalPaginas} en staging: archivo vacío o corrupto.")
                        End If

                        Dim completadas = Interlocked.Increment(contadorPaginasProcesadas)
                        If (completadas Mod 10 = 0 OrElse completadas = numTotalPaginas) AndAlso bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                            Dim pct = CInt((completadas / Math.Max(1, numTotalPaginas)) * 100)
                            bgWorker.ReportProgress(pct, $"STATUS|{nombreBase} ({pesoMB:0.1} MB): {completadas:#,##0}/{numTotalPaginas:#,##0} págs procesadas ({hilosParalelos} hilos)")
                        End If
                    End Sub)
                Else
                    ' MODO SECUENCIAL POR PÁGINA (Cada hilo ya está convirtiendo un PDF completo diferente en paralelo)
                    For i As Integer = 0 To collection.Count - 1
                        If token.IsCancellationRequested Then Throw New OperationCanceledException()

                        Dim page = collection(i)
                        page.Format = MagickFormat.Tiff
                        page.Settings.Compression = CompressionMethod.LZW
                        page.Alpha(AlphaOption.Remove)

                        Try
                            page.Quantize(New QuantizeSettings() With {
                                .Colors = 2,
                                .ColorSpace = ColorSpace.Gray
                            })
                        Catch
                            page.ColorSpace = ColorSpace.Gray
                        End Try

                        Dim tiffLocal = Path.Combine(stagingCarpeta, $"{nombreBase}_{i + 1}.tiff")
                        page.Write(tiffLocal)

                        Dim fiLocal As New FileInfo(tiffLocal)
                        If Not fiLocal.Exists OrElse fiLocal.Length < 16 Then
                            Throw New IOException($"Fallo al generar la página {i + 1} de {totalPaginas} en staging: archivo vacío o corrupto.")
                        End If
                    Next
                End If
            End Using

            If token.IsCancellationRequested Then Throw New OperationCanceledException()

            ' 3. Asegurar carpeta destino en red
            SyncLock locker
                If Not Directory.Exists(carpetaDestino) Then
                    Directory.CreateDirectory(carpetaDestino)
                End If
            End SyncLock

            ' 4. Transferencia de TIFFs listos en ráfaga
            For Each tiffFile In Directory.GetFiles(stagingCarpeta, "*.tiff")
                If token.IsCancellationRequested Then
                    LimpiarCarpetaIncompleta(carpetaDestino)
                    Throw New OperationCanceledException()
                End If
                Dim destinoFinal = Path.Combine(carpetaDestino, Path.GetFileName(tiffFile))
                File.Copy(tiffFile, destinoFinal, True)
            Next

        Catch ex As OperationCanceledException
            LimpiarCarpetaIncompleta(carpetaDestino)
            Throw
        Catch ex As Exception
            LimpiarCarpetaIncompleta(carpetaDestino)
            Throw
        Finally
            ' Limpieza de carpeta temporal de staging
            Try
                If Directory.Exists(stagingCarpeta) Then
                    Directory.Delete(stagingCarpeta, True)
                End If
            Catch
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Obtiene rápidamente el número total de páginas de un PDF sin descomprimirlo en RAM.
    ''' Utiliza iText 7 como método principal (lectura de trailer/catálogo en ~0.5ms) con fallback a ImageMagick Ping.
    ''' </summary>
    Private Function ObtenerTotalPaginasPdf(rutaPdf As String) As Integer
        Try
            Using reader As New PdfReader(rutaPdf)
                Using doc As New PdfDocument(reader)
                    Return doc.GetNumberOfPages()
                End Using
            End Using
        Catch
            Try
                Using col As New MagickImageCollection()
                    col.Ping(rutaPdf)
                    Return col.Count
                End Using
            Catch
                Return 0
            End Try
        End Try
    End Function

    ''' <summary>
    ''' Verifica de forma rápida y física si un archivo PDF ya fue transferido y completado al 100% en el destino:
    ''' 1. Comprueba si existe la carpeta del PDF en el directorio destino.
    ''' 2. Comprueba que contenga archivos TIFF.
    ''' 3. Ejecuta la validación física rápida de páginas y cabecera mágica (sin páginas faltantes ni corruptas).
    ''' </summary>
    Private Function EstaPdfYaProcesadoYCompleto(origenBase As String, destinoBase As String, archivoPdf As String, ByRef totalPaginas As Integer) As Boolean
        Try
            Dim rutaRelativaArchivo = ObtenerRutaRelativa(origenBase, archivoPdf)
            Dim directorioRelativo = Path.GetDirectoryName(rutaRelativaArchivo)
            Dim destinoDirectorioFinal = Path.Combine(destinoBase, directorioRelativo)
            Dim nombreSinExt = Path.GetFileNameWithoutExtension(archivoPdf)
            Dim carpetaPdfDestino = Path.Combine(destinoDirectorioFinal, nombreSinExt)

            If Not Directory.Exists(carpetaPdfDestino) Then
                Return False
            End If

            ' Si la carpeta destino existe, verificar si tiene archivos TIFF físicos
            Dim archivosTiff = Directory.GetFiles(carpetaPdfDestino, "*.tiff")
            If archivosTiff.Length = 0 Then
                Return False
            End If

            ' Obtener las páginas del PDF original para comparar conteo exacto
            Dim paginasEsperadas = ObtenerTotalPaginasPdf(archivoPdf)
            If paginasEsperadas <= 0 Then
                Return False
            End If

            totalPaginas = paginasEsperadas

            ' Ejecutar la verificación física de integridad y cantidad exacta
            Dim dummyError As String = ""
            If ValidarIntegridadFisicaTiff(carpetaPdfDestino, nombreSinExt, paginasEsperadas, dummyError) Then
                Return True
            End If

            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Realiza una verificación física rápida y exhaustiva en disco para garantizar:
    ''' 1. Que la carpeta destino exista.
    ''' 2. Que el conteo físico de archivos TIFF coincida exactamente con las páginas esperadas (sin páginas faltantes ni de más).
    ''' 3. Que cada archivo consecutivo ({nombreBase}_{i}.tiff) exista físicamente en disco.
    ''' 4. Que ningún archivo esté vacío o incompleto (tamaño > 0 y cabecera mínima >= 16 bytes).
    ''' 5. Que la cabecera mágica corresponda a un archivo TIFF 6.0 estándar ("II*" little-endian o "MM*" big-endian).
    ''' 6. Que la imagen sea completamente legible (validación rápida de metadatos MagickImageInfo sin decodificar píxeles).
    ''' </summary>
    Private Function ValidarIntegridadFisicaTiff(carpetaDestino As String, nombreBase As String, totalPaginasEsperadas As Integer, ByRef mensajeError As String) As Boolean
        Try
            If Not Directory.Exists(carpetaDestino) Then
                mensajeError = "La carpeta de destino no existe físicamente en el almacenamiento."
                Return False
            End If

            If totalPaginasEsperadas <= 0 Then
                mensajeError = "El número de páginas esperadas es 0 o inválido."
                Return False
            End If

            ' 1. Obtener todos los archivos TIFF en la carpeta destino
            Dim archivosTiff = Directory.GetFiles(carpetaDestino, "*.tiff")

            ' 2. Validación estricta de cantidad: no pueden haber menos páginas ni archivos sobrantes
            If archivosTiff.Length <> totalPaginasEsperadas Then
                mensajeError = $"Discrepancia en páginas físicas: se esperaban {totalPaginasEsperadas} páginas pero hay {archivosTiff.Length} archivos TIFF en disco."
                Return False
            End If

            ' 3. Validación archivo por archivo de cada página esperada
            For i As Integer = 1 To totalPaginasEsperadas
                Dim archivoEsperado = Path.Combine(carpetaDestino, $"{nombreBase}_{i}.tiff")

                If Not File.Exists(archivoEsperado) Then
                    mensajeError = $"Falta la página física requerida: {nombreBase}_{i}.tiff"
                    Return False
                End If

                Dim fi As New FileInfo(archivoEsperado)
                If fi.Length < 16 Then
                    mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} está dañado o vacío (tamaño: {fi.Length} bytes)."
                    Return False
                End If

                ' 4. Verificación ultrarrápida de Magic Bytes TIFF (TIFF 6.0: II* o MM*)
                Using fs As New FileStream(archivoEsperado, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Dim header(3) As Byte
                    Dim leidos = fs.Read(header, 0, 4)
                    If leidos < 4 Then
                        mensajeError = $"No se pudo leer la cabecera del archivo {Path.GetFileName(archivoEsperado)}."
                        Return False
                    End If

                    Dim esLittleEndian = (header(0) = &H49 AndAlso header(1) = &H49 AndAlso header(2) = &H2A AndAlso header(3) = &H0)
                    Dim esBigEndian = (header(0) = &H4D AndAlso header(1) = &H4D AndAlso header(2) = &H0 AndAlso header(3) = &H2A)

                    If Not esLittleEndian AndAlso Not esBigEndian Then
                        mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} no contiene una cabecera TIFF válida (archivo corrupto)."
                        Return False
                    End If
                End Using

                ' 5. Validación rápida de estructura de imagen con ImageMagick (Ping de metadatos sin descomprimir píxeles)
                Try
                    Dim info As New MagickImageInfo(archivoEsperado)
                    If info.Width <= 0 OrElse info.Height <= 0 OrElse info.Format <> MagickFormat.Tiff Then
                        mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} tiene dimensiones o formato TIFF inválidos ({info.Width}x{info.Height})."
                        Return False
                    End If
                Catch exMagick As Exception
                    mensajeError = $"El archivo {Path.GetFileName(archivoEsperado)} está dañado e ilegible: {exMagick.Message}"
                    Return False
                End Try
            Next

            mensajeError = ""
            Return True

        Catch ex As Exception
            mensajeError = "Excepción durante la verificación física de archivos: " & ex.Message
            Return False
        End Try
    End Function

    Private Sub LimpiarCarpetaIncompleta(carpeta As String)
        Try
            If Not String.IsNullOrEmpty(carpeta) AndAlso Directory.Exists(carpeta) Then
                Directory.Delete(carpeta, True)
                EscribirLog($"LIMPIEZA | Eliminada carpeta incompleta: {carpeta}")
            End If
        Catch ex As Exception
            Debug.WriteLine("Error al limpiar carpeta incompleta: " & ex.Message)
        End Try
    End Sub

    Private Sub LimpiarTodosLosIncompletosEnVuelo()
        For Each kvp In ArchivosEnProceso
            LimpiarCarpetaIncompleta(kvp.Value)
        Next
        ArchivosEnProceso.Clear()
    End Sub

    Private Function ObtenerRutaRelativa(basePath As String, fullPath As String) As String
        Try
            If Not basePath.EndsWith("\") Then
                basePath &= "\"
            End If
            Dim uriBase As New Uri(basePath)
            Dim uriFull As New Uri(fullPath)
            Dim relativeUri As Uri = uriBase.MakeRelativeUri(uriFull)
            Dim relativePath As String = Uri.UnescapeDataString(relativeUri.ToString())
            Return relativePath.Replace("/"c, "\"c)
        Catch ex As Exception
            Return fullPath.Replace(basePath, "").TrimStart("\"c)
        End Try
    End Function

    Private Sub bgWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles bgWorker.ProgressChanged
        If e.UserState IsNot Nothing Then
            Dim str = e.UserState.ToString()
            If str.StartsWith("STATUS|") Then
                Me.Text = "Transferir a TIFF — " & str.Substring(7)
            ElseIf str.Contains("|") Then
                Dim partes = str.Split("|"c)
                dgvBitacora.Rows.Add(partes(0).Trim(), partes(1).Trim())
                If dgvBitacora.RowCount > 0 Then
                    dgvBitacora.FirstDisplayedScrollingRowIndex = dgvBitacora.RowCount - 1
                End If
            Else
                dgvBitacora.Rows.Add(str, "Procesado")
                If dgvBitacora.RowCount > 0 Then
                    dgvBitacora.FirstDisplayedScrollingRowIndex = dgvBitacora.RowCount - 1
                End If
            End If
        End If
    End Sub

    Private Sub EscribirLog(mensaje As String)
        Try
            Dim rutaLog As String = "C:\LogsTIFF"
            If Not Directory.Exists(rutaLog) Then
                Directory.CreateDirectory(rutaLog)
            End If
            Dim archivoLog As String = Path.Combine(rutaLog, "log_" & DateTime.Now.ToString("yyyyMMdd") & ".txt")
            Dim linea As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " | " & mensaje
            File.AppendAllText(archivoLog, linea & Environment.NewLine)
        Catch
        End Try
    End Sub

    Private Sub ConfigurarGrid()
        With dgvBitacora
            .Columns.Clear()
            .ColumnCount = 2
            .Columns(0).Name = "Archivo"
            .Columns(1).Name = "Estado"
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        End With
    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click
    End Sub

End Class