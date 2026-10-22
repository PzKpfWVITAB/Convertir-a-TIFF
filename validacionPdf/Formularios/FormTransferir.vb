Imports System.Collections.Concurrent
Imports System.ComponentModel
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
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
    Private Shared ReadOnly ProcesosActivos As New ConcurrentDictionary(Of Integer, Process)()

    ''' <summary>
    ''' Mata de forma inmediata y fulminante los subprocesos de conversión activos pertenecientes a ESTA instancia,
    ''' sin afectar otras instancias en ejecución simultánea en el servidor.
    ''' </summary>
    Public Shared Sub MatarTodosLosProcesosDeConversion()
        For Each kvp In ProcesosActivos
            Try
                Dim p = kvp.Value
                If Not p.HasExited Then
                    p.Kill()
                End If
            Catch
            End Try
        Next
        ProcesosActivos.Clear()
    End Sub

    Public Class TransferData
        Public Property Origen As String
        Public Property Destino As String
    End Class

    Private Sub FormTransferir_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ConfigurarGrid()

        ' Aplicar configuración dinámica de límites de hardware y búfer de staging
        ModuloRecursos.AplicarLimitesMagickNET()
        ModuloRecursos.LimpiarCacheHuerfana(False)

        ' DETECTOR E INSTALADOR AUTOMÁTICO DE MUPDF (OBLIGATORIO Y EXCLUSIVO)
        Dim mupdfListo = ModuloRecursos.AsegurarMuPdfInstalado(Me)
        If Not mupdfListo Then
            dgvBitacora.Rows.Add("ERROR MOTOR", "No se encontró ni se pudo descargar MuPDF (mutool.exe)")
            btnTransferir.Enabled = False
        End If

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

    Private Sub btnReparar_Click(sender As Object, e As EventArgs) Handles btnReparar.Click
        Dim frm As New FormReparacion(rutaOrigenActual, rutaDestinoActual)
        frm.ShowDialog(Me)
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
        ActualizarResumenIndiceEnUi(rutaOrigenActual)
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
        ActualizarResumenIndiceEnUi(rutaOrigenActual)
    End Sub

    Private cancelSourceIndexar As CancellationTokenSource = Nothing
    Private estaIndexando As Boolean = False

    Private Sub ActualizarResumenIndiceEnUi(ruta As String)
        If String.IsNullOrWhiteSpace(ruta) Then
            lblResumenIndice.Text = "Índice: Seleccione un origen"
            lblResumenIndice.ForeColor = Color.FromArgb(100, 100, 100)
            Exit Sub
        End If

        lblResumenIndice.Text = "Índice: Consultando..."
        lblResumenIndice.ForeColor = Color.FromArgb(100, 100, 100)

        Task.Run(Sub()
                     Dim resumen = ModuloIndiceOrigen.ObtenerResumenIndice(ruta)
                     Me.BeginInvoke(Sub()
                                        If rutaOrigenActual = ruta Then
                                            lblResumenIndice.Text = resumen.ToString()
                                            If resumen.Existe Then
                                                lblResumenIndice.ForeColor = Color.FromArgb(30, 120, 50)
                                            Else
                                                lblResumenIndice.ForeColor = Color.FromArgb(160, 90, 20)
                                            End If
                                        End If
                                    End Sub)
                 End Sub)
    End Sub

    Private Async Sub btnIndexarOrigen_Click(sender As Object, e As EventArgs) Handles btnIndexarOrigen.Click
        If estaIndexando Then
            If cancelSourceIndexar IsNot Nothing Then
                Dim resp = MessageBox.Show("¿Desea cancelar el proceso de indexación en curso?", "Cancelar Indexado", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If resp = DialogResult.Yes Then
                    cancelSourceIndexar.Cancel()
                    btnIndexarOrigen.Text = "Cancelando..."
                    btnIndexarOrigen.Enabled = False
                End If
            End If
            Exit Sub
        End If

        Dim ruta = rutaOrigenActual
        If String.IsNullOrWhiteSpace(ruta) OrElse Not Directory.Exists(ruta) Then
            MessageBox.Show("Seleccione una carpeta de origen válida antes de indexar.", "Origen no seleccionado", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If

        Dim resumenActual = ModuloIndiceOrigen.ObtenerResumenIndice(ruta)
        Dim msgPregunta = $"Se creará/reconstruirá el índice de PDFs de origen en la base de datos para la ruta:{vbCrLf}{vbCrLf}" &
                          $"{ruta}{vbCrLf}{vbCrLf}"
        If resumenActual.Existe Then
            msgPregunta &= $"Actualmente hay {resumenActual.TotalPdfs:#,##0} PDFs indexados en esta ubicación.{vbCrLf}" &
                           "Al continuar, se actualizará el índice de inventario con los archivos actuales.{vbCrLf}{vbCrLf}"
        Else
            msgPregunta &= "Esta ubicación no tiene índice registrado todavía.{vbCrLf}{vbCrLf}"
        End If
        msgPregunta &= "¿Desea iniciar la indexación ahora?"

        If MessageBox.Show(msgPregunta, "Confirmar Indexado de Origen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Exit Sub
        End If

        estaIndexando = True
        btnIndexarOrigen.Text = "⏹ Cancelar Indexado"
        btnTransferir.Enabled = False
        cancelSourceIndexar = New CancellationTokenSource()
        Dim token = cancelSourceIndexar.Token

        Dim hilos = ModuloRecursos.ObtenerHilosOptimos()
        Try
            Dim resumenFinal As ModuloIndiceOrigen.ResumenIndice = Nothing
            Await Task.Run(Function()
                               Return ModuloIndiceOrigen.ReindexarDirectorio(
                                   ruta,
                                   hilos,
                                   token,
                                   Sub(proc, tot, fase)
                                       Me.BeginInvoke(Sub()
                                                          If tot > 0 Then
                                                              lblResumenIndice.Text = $"{fase} {proc:#,##0}/{tot:#,##0}"
                                                          Else
                                                              lblResumenIndice.Text = fase
                                                          End If
                                                      End Sub)
                                   End Sub
                               )
                           End Function, token).ContinueWith(Sub(t)
                                                                 If t.IsFaulted Then
                                                                     Throw t.Exception.InnerException
                                                                 ElseIf Not t.IsCanceled Then
                                                                     resumenFinal = t.Result
                                                                 End If
                                                             End Sub)

            If resumenFinal IsNot Nothing Then
                lblResumenIndice.Text = resumenFinal.ToString()
                lblResumenIndice.ForeColor = Color.FromArgb(30, 120, 50)
                Dim msgExito = $"Inventario de PDFs indexado exitosamente:{vbCrLf}{vbCrLf}" &
                               $"• Total PDFs en inventario: {resumenFinal.TotalPdfs:#,##0}{vbCrLf}" &
                               $"• Ubicación: {ruta}"
                If resumenFinal.TotalPaginas > 0 Then
                    msgExito &= $"{vbCrLf}• Páginas registradas: {resumenFinal.TotalPaginas:#,##0}"
                End If
                MessageBox.Show(msgExito, "Índice de Inventario Actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As OperationCanceledException
            lblResumenIndice.Text = "Indexación cancelada por el usuario."
            lblResumenIndice.ForeColor = Color.FromArgb(160, 40, 40)
        Catch ex As Exception
            lblResumenIndice.Text = "Error en indexación: " & ex.Message
            lblResumenIndice.ForeColor = Color.FromArgb(180, 20, 20)
            MessageBox.Show("Error durante la indexación: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            estaIndexando = False
            btnIndexarOrigen.Text = "📑 Indexar Origen"
            btnIndexarOrigen.Enabled = True
            btnTransferir.Enabled = True
            If cancelSourceIndexar IsNot Nothing Then
                cancelSourceIndexar.Dispose()
                cancelSourceIndexar = Nothing
            End If
            ActualizarResumenIndiceEnUi(rutaOrigenActual)
        End Try
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
        Try
            If cancelSourceTransfer IsNot Nothing AndAlso Not cancelSourceTransfer.IsCancellationRequested Then
                cancelSourceTransfer.Cancel()
            End If
        Catch
        End Try

        Try
            If bgWorker IsNot Nothing AndAlso bgWorker.IsBusy Then
                bgWorker.CancelAsync()
            End If
        Catch
        End Try

        EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | CANCELACIÓN INMEDIATA SOLICITADA POR EL USUARIO")

        ' 1. Matar fulminantemente todos los subprocesos en segundo plano (gswin64c, mutool, gs)
        MatarTodosLosProcesosDeConversion()

        ' 2. Limpieza inmediata de todas las carpetas destino incompletas en vuelo
        LimpiarTodosLosIncompletosEnVuelo()

        ' 3. Limpieza inmediata de cachés y carpetas temporales de esta instancia
        ModuloRecursos.LimpiarCacheHuerfana(True)

        EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | CANCELACIÓN COMPLETADA | Subprocesos cerrados y temporales eliminados.")
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

        ' RESOLVER DESTINO EFECTIVO CON LA JERARQUÍA OFICIAL DE ENTREGABLES
        Dim destinoEfectivo As String = ResolverDestinoEfectivo(origen, destino)

        ' 1. Cargar el índice de origen primero (O(1) en MySQL en ~10 ms)
        If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
            bgWorker.ReportProgress(0, "STATUS|Consultando índice permanente de origen en base de datos...")
        End If

        Dim mapaIndiceOrigen As Dictionary(Of String, ModuloIndiceOrigen.InfoIndicePdf) = Nothing
        Try
            mapaIndiceOrigen = ModuloIndiceOrigen.CargarIndiceDirectorio(origen)
        Catch
            mapaIndiceOrigen = Nothing
        End Try

        ' Cargar también la bitácora completa de la BD en memoria (O(1)) para pre-chequeo instantáneo
        Dim mapaBdTransfer As Dictionary(Of String, ModuloConversionTiff.InfoRegistroBd) = Nothing
        Try
            mapaBdTransfer = ModuloConversionTiff.CargarDiccionarioBitacoraEnMemoria()
        Catch
            mapaBdTransfer = Nothing
        End Try

        ' Obtener dinámicamente hilos óptimos (con 10% libre de reserva: 48 hilos para 54 núcleos)
        Dim maxHilos As Integer = ModuloRecursos.ObtenerHilosOptimos()
        Dim cancelToken As CancellationToken = If(cancelSourceTransfer IsNot Nothing, cancelSourceTransfer.Token, CancellationToken.None)

        ' 2. Obtener lista de archivos a procesar:
        ' Si la carpeta ya está indexada en MySQL, usamos directamente los archivos del índice (0 ms, sin escanear SMB por red)
        Dim archivos As FileInfo() = Nothing
        If mapaIndiceOrigen IsNot Nothing AndAlso mapaIndiceOrigen.Count > 0 Then
            Dim rutasIndexadas = mapaIndiceOrigen.Keys.Where(Function(k) k.Contains(Path.DirectorySeparatorChar) OrElse k.Contains("/"c)).ToList()
            archivos = rutasIndexadas.Select(Function(r) New FileInfo(r)).ToArray()
            EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | INDICE ORIGEN ACTIVO | Se cargaron {archivos.Length:#,##0} PDFs directamente de la base de datos (0 ms por red SMB).")
        End If

        ' Si no había índice previo, realizar escaneo en disco/red en paralelo aprovechando todos los núcleos ({maxHilos} hilos)
        If archivos Is Nothing OrElse archivos.Length = 0 Then
            If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                bgWorker.ReportProgress(0, $"STATUS|Buscando archivos PDF en carpeta de origen a alta velocidad ({maxHilos} núcleos)...")
            End If
            archivos = ModuloIndiceOrigen.EnumerarArchivosPdfParalelo(origen, cancelToken, maxHilos)
        End If

        Dim total As Integer = archivos.Length
        Dim procesados As Integer = 0
        Dim delegacionSeleccionada As String = New DirectoryInfo(destinoEfectivo).Name

        If total = 0 Then
            EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | No se encontraron archivos PDF en origen: {origen}")
            Exit Sub
        End If

        ' VERIFICAR MOTOR EXCLUSIVO MUPDF (OBLIGATORIO)
        Dim rutaMu = ModuloRecursos.ObtenerRutaEjecutableMuPdf()
        If String.IsNullOrEmpty(rutaMu) OrElse Not File.Exists(rutaMu) Then
            ModuloRecursos.AsegurarMuPdfInstalado()
            rutaMu = ModuloRecursos.ObtenerRutaEjecutableMuPdf(True)
        End If

        If String.IsNullOrEmpty(rutaMu) OrElse Not File.Exists(rutaMu) Then
            Dim msgError = "El motor exclusivo de alta velocidad MuPDF (mutool.exe) no está instalado y no se pudo obtener automáticamente. No se permite el uso de motores obsoletos."
            EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ERROR FATAL | {msgError}")
            Throw New InvalidOperationException(msgError)
        End If

        EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | INICIO | Motor exclusivo: MuPDF Extremo ({Path.GetFileName(rutaMu)}) | Origen: {origen} | Destino efectivo: {destinoEfectivo} | Hilos paralelos dinámicos: {maxHilos}")

        ' Limpieza proactiva de residuos huérfanos antiguos sin afectar otras instancias activas
        ModuloRecursos.LimpiarCacheHuerfana(False)

        ' =========================================================================
        ' FASE DE PRE-VERIFICACIÓN: Detectar PDFs ya procesados completos en destino
        ' =========================================================================
        If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
            bgWorker.ReportProgress(0, "STATUS|Iniciando pre-verificación en destino a alta velocidad...")
        End If

        ' Caché concurrente en RAM de subdirectorios existentes por cada carpeta padre en destino.
        Dim cacheDirectoriosDestino As New ConcurrentDictionary(Of String, Lazy(Of HashSet(Of String)))(StringComparer.OrdinalIgnoreCase)

        Dim bagPendientes As New ConcurrentBag(Of FileInfo)()
        Dim bagOmitidos As New ConcurrentBag(Of KeyValuePair(Of String, Integer))()
        Dim contadorChequeados As Integer = 0
        Dim usuarioPrecheck = If(ModuloUsuarios.UsuarioActual IsNot Nothing, ModuloUsuarios.UsuarioActual.NombreUsuario, Environment.UserName)

        Dim hilosPrechequeo = Math.Max(4, Math.Min(Environment.ProcessorCount * 2, ModuloRecursos.ObtenerHilosRedOptimos()))
        Dim poptPrechequeo As New ParallelOptions With {
            .MaxDegreeOfParallelism = hilosPrechequeo,
            .CancellationToken = cancelToken
        }

        Parallel.ForEach(archivos, poptPrechequeo, Sub(archivoInfo, state)
            If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
                state.Stop()
                Exit Sub
            End If

            Dim pagsCompletas As Integer = 0
            If EstaPdfYaProcesadoYCompleto(origen, destinoEfectivo, archivoInfo.FullName, pagsCompletas, cacheDirectoriosDestino, mapaIndiceOrigen, mapaBdTransfer) Then
                bagOmitidos.Add(New KeyValuePair(Of String, Integer)(archivoInfo.FullName, pagsCompletas))

                ' Auto-sanación de BD: si está 100% completo en disco pero faltaba su confirmación OK en BD, registrarlo
                Dim yaEnBdOk As Boolean = False
                Dim nomSinExt = Path.GetFileNameWithoutExtension(archivoInfo.FullName)
                Dim rutaRel = ObtenerRutaRelativa(origen, archivoInfo.FullName)
                Dim dirRel = NormalizarRutaRelativa(Path.GetDirectoryName(rutaRel))
                Dim carpDest = Path.Combine(destinoEfectivo, dirRel, nomSinExt)

                If mapaBdTransfer IsNot Nothing Then
                    Dim infoBd As ModuloConversionTiff.InfoRegistroBd = Nothing
                    If mapaBdTransfer.TryGetValue(carpDest.TrimEnd("\"c, "/"c), infoBd) OrElse
                       mapaBdTransfer.TryGetValue(archivoInfo.FullName, infoBd) OrElse
                       mapaBdTransfer.TryGetValue(nomSinExt, infoBd) OrElse
                       mapaBdTransfer.TryGetValue(archivoInfo.Name, infoBd) Then
                        If infoBd IsNot Nothing AndAlso infoBd.Encontrado AndAlso String.Equals(infoBd.Estatus, "OK", StringComparison.OrdinalIgnoreCase) Then
                            yaEnBdOk = True
                        End If
                    End If
                End If

                If Not yaEnBdOk AndAlso pagsCompletas > 0 Then
                    Dim mAnio = System.Text.RegularExpressions.Regex.Match(nomSinExt, "\b(19\d{2}|20\d{2})\b")
                    Dim anioStr = If(mAnio.Success, mAnio.Value, Nothing)
                    ModuloBitacoraAsync.EncolarTransferenciaTiff(
                        origen,
                        Path.GetDirectoryName(archivoInfo.FullName),
                        carpDest,
                        archivoInfo.FullName,
                        archivoInfo.Name,
                        nomSinExt,
                        "",
                        "OK",
                        pagsCompletas,
                        delegacionSeleccionada,
                        usuarioPrecheck,
                        anioStr
                    )
                End If
            Else
                bagPendientes.Add(archivoInfo)
            End If

            Dim actual = Interlocked.Increment(contadorChequeados)
            If (actual Mod 100 = 0 OrElse actual = total) AndAlso bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                Dim pct = CInt((actual / Math.Max(1, total)) * 10)
                bgWorker.ReportProgress(pct, $"STATUS|Pre-chequeo ({hilosPrechequeo} hilos): {actual:#,##0}/{total:#,##0} analizados ({bagOmitidos.Count:#,##0} ya listos)")
            End If
        End Sub)

        If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
            Exit Sub
        End If

        ' Función rápida para obtener el peso del archivo (del índice en memoria si está disponible, evitando I/O de red)
        Dim obtenerPesoBytes = Function(f As FileInfo) As Long
            Dim infoIdx As ModuloIndiceOrigen.InfoIndicePdf = Nothing
            If mapaIndiceOrigen IsNot Nothing Then
                If mapaIndiceOrigen.TryGetValue(f.FullName, infoIdx) AndAlso infoIdx.Tamano > 0 Then
                    Return infoIdx.Tamano
                ElseIf mapaIndiceOrigen.TryGetValue(f.Name, infoIdx) AndAlso infoIdx.Tamano > 0 Then
                    Return infoIdx.Tamano
                End If
            End If
            Try
                Return f.Length
            Catch
                Return 0L
            End Try
        End Function

        ' ORDENAR ESTRICTAMENTE DESCENDENTE POR PESO ORIGINAL DEL PDF (MÁS PESADOS PRIMERO, MENOS PESADOS AL FINAL)
        Dim listaPendientes = bagPendientes.OrderByDescending(Function(f) obtenerPesoBytes(f)).ToList()
        Dim listaOmitidos = bagOmitidos.ToList()
        Dim totalOmitidos = listaOmitidos.Count
        Dim totalPendientes = listaPendientes.Count

        Dim pesoMaxMB = If(totalPendientes > 0, (obtenerPesoBytes(listaPendientes.First()) / (1024.0 * 1024.0)), 0)
        Dim pesoMinMB = If(totalPendientes > 0, (obtenerPesoBytes(listaPendientes.Last()) / (1024.0 * 1024.0)), 0)

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

        ' Si todos los archivos ya están completos, terminar inmediatamente asegurando vaciado de bitácora
        If totalPendientes = 0 Then
            ModuloBitacoraAsync.EsperarVaciado(15000)
            If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                bgWorker.ReportProgress(100, $"Todos los archivos ({total:#,##0}) ya están completos en destino | Finalizado sin pendientes")
            End If
            EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | FIN | Todos los {total} archivos ya estaban completos en destino. Ninguno requirió conversión.")
            Exit Sub
        End If

        If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
            bgWorker.ReportProgress(10, $"COMIENZO DE CONVERSIÓN | {totalPendientes:#,##0} pendientes (Ordenados por peso: {pesoMaxMB:0.1} MB máx -> {pesoMinMB:0.1} MB mín)")
        End If

        ' =========================================================================
        ' PIPELINE CONTINUO DINÁMICO MULTIHILO (CERO BARRERAS / CERO HILOS PEREZOSOS)
        ' =========================================================================
        ' Regla de Oro:
        ' 1. PDFs Grandes (> 30 MB): Máximo 20 documentos concurrentes en vuelo (protección de RAM/staging).
        ' 2. Capacidad Total: Hasta maxHilos (48+ hilos en servidores de 54 núcleos / 1 hilo por núcleo).
        ' 3. CERO ESPERAS: Si un hilo termina un archivo (pesado o ligero), toma INMEDIATAMENTE el siguiente
        '    disponible de la cola en 0 ms. No espera bloques ni espera a que otros hilos terminen.
        ' 4. Transición fluida: Los núcleos no utilizados por archivos pesados procesan archivos ligeros
        '    simultáneamente. Conforme terminan los pesados, los hilos se suman a los ligeros al instante.
        ' =========================================================================
        Dim limiteGrandeBytes As Long = ModuloRecursos.LimitePdfGrandeBytes
        Dim colaGrandes As New ConcurrentQueue(Of FileInfo)(listaPendientes.Where(Function(f) obtenerPesoBytes(f) > limiteGrandeBytes))
        Dim colaPequenos As New ConcurrentQueue(Of FileInfo)(listaPendientes.Where(Function(f) obtenerPesoBytes(f) <= limiteGrandeBytes))

        Dim totalGrandes = colaGrandes.Count
        Dim totalPequenos = colaPequenos.Count
        Dim maxGrandesEnParalelo As Integer = Math.Max(1, Math.Min(20, maxHilos))
        Dim hilosEncodingPorDocGrande As Integer = Math.Max(2, Math.Min(4, Math.Max(2, Environment.ProcessorCount \ maxGrandesEnParalelo)))

        EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | PIPELINE DINÁMICO ACTIVO | Total pendientes: {totalPendientes} | Grandes (>30MB): {totalGrandes} (Tope simultáneo: {maxGrandesEnParalelo}) | Ligeros: {totalPequenos} | Hilos de trabajo: {maxHilos} (1 por núcleo)")

        If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
            bgWorker.ReportProgress(10, $"PIPELINE DINÁMICO ACTIVO | {totalGrandes} pesados (máx {maxGrandesEnParalelo} simultáneos) + {totalPequenos} ligeros a {maxHilos} hilos (0 esperas)")
        End If

        Dim grandesEnVuelo As Integer = 0
        Dim lockerCola As New Object()
        Dim tareasConsumidoras As New List(Of Task)()

        Try
            For h As Integer = 1 To maxHilos
                tareasConsumidoras.Add(Task.Run(Sub()
                    While Not canceladoPorUsuario AndAlso Not cancelToken.IsCancellationRequested
                        Dim fileAProcesar As FileInfo = Nothing
                        Dim esGrande As Boolean = False

                        SyncLock lockerCola
                            While Not canceladoPorUsuario AndAlso Not cancelToken.IsCancellationRequested
                                ' Condición A: No queda ningún trabajo pendiente en ninguna cola
                                If colaGrandes.IsEmpty AndAlso colaPequenos.IsEmpty Then
                                    Exit While
                                End If

                                ' Condición B: Hay archivos grandes y no hemos alcanzado el tope de 20
                                If grandesEnVuelo < maxGrandesEnParalelo AndAlso colaGrandes.TryDequeue(fileAProcesar) Then
                                    grandesEnVuelo += 1
                                    esGrande = True
                                    Exit While
                                End If

                                ' Condición C: Tomar archivo ligero si hay disponible
                                If colaPequenos.TryDequeue(fileAProcesar) Then
                                    esGrande = False
                                    Exit While
                                End If

                                ' Condición D: Si quedan archivos grandes pero ya hay 20 en vuelo y la cola pequeña está vacía,
                                ' esperar a que se libere un slot de archivo grande (máximo 50ms)
                                Monitor.Wait(lockerCola, 50)
                            End While
                        End SyncLock

                        ' Si no se obtuvo archivo, significa que ya no hay pendientes en ninguna cola
                        If fileAProcesar Is Nothing Then
                            Exit While
                        End If

                        ' Procesamiento inmediato sin esperas
                        Try
                            If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
                                Exit While
                            End If

                            Dim paginas As Integer = 0
                            Dim pesoMB As Double = obtenerPesoBytes(fileAProcesar) / (1024.0 * 1024.0)
                            Dim nombreSinExt = Path.GetFileNameWithoutExtension(fileAProcesar.FullName)

                            If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
                                Dim pctActual = CInt(10 + ((procesados / Math.Max(1, totalPendientes)) * 90))
                                Dim etiquetaTipo = If(esGrande, $"PDF Grande [{grandesEnVuelo}/{maxGrandesEnParalelo}]", $"PDF Estándar [{maxHilos} hilos]")
                                bgWorker.ReportProgress(pctActual, $"STATUS|Procesando {etiquetaTipo}: {nombreSinExt} ({pesoMB:0.1} MB)")
                            End If

                            Dim hilosDoc = If(esGrande, hilosEncodingPorDocGrande, 1)
                            Dim estatus = ProcesarUnArchivoPdf(fileAProcesar, origen, destinoEfectivo, delegacionSeleccionada, cancelToken, esGrande, hilosDoc, paginas)
                            Dim actualProcesados = Interlocked.Increment(procesados)
                            Dim porcentaje = CInt(10 + ((actualProcesados / Math.Max(1, totalPendientes)) * 90))

                            If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress AndAlso Not canceladoPorUsuario Then
                                bgWorker.ReportProgress(porcentaje, $"{nombreSinExt} | {estatus} ({pesoMB:0.1} MB - {paginas} págs - {actualProcesados}/{totalPendientes})")
                            End If
                        Finally
                            If esGrande Then
                                SyncLock lockerCola
                                    grandesEnVuelo -= 1
                                    Monitor.PulseAll(lockerCola)
                                End SyncLock
                            End If
                        End Try
                    End While
                End Sub))
            Next

            Task.WaitAll(tareasConsumidoras.ToArray())
        Catch exOp As OperationCanceledException
        Catch exGen As Exception
            EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ERROR PIPELINE | {exGen.Message}")
        End Try

        ' Si hubo cancelación, barrer y asegurar limpieza de cualquier archivo que estuviera en vuelo
        If canceladoPorUsuario Then
            LimpiarTodosLosIncompletosEnVuelo()
        End If

        ' Esperar que la cola de bitácora confirme todas las transacciones en MySQL
        ModuloBitacoraAsync.EsperarVaciado(15000)
        ModuloRecursos.LimpiarCacheHuerfana(True)
        EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | FIN PROCESO | Origen: {origen} | Destino efectivo: {destinoEfectivo} | Cancelado: {canceladoPorUsuario}")
    End Sub

    ''' <summary>
    ''' Procesa un único archivo PDF con hasta 3 reintentos automáticos ante microcortes o errores transitorios de I/O:
    ''' gestiona rutas relativas, llamadas de conversión, verificación física, registro en BD y limpieza de incompletos.
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
        Dim pesoMB As Double = 0
        Try
            pesoMB = fileInfo.Length / (1024.0 * 1024.0)
        Catch
        End Try

        If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
            Return "CANCELADO"
        End If

        Try
            Dim rutaRelativaArchivo As String = ObtenerRutaRelativa(origen, archivo)
            Dim directorioRelativo As String = NormalizarRutaRelativa(Path.GetDirectoryName(rutaRelativaArchivo))
            Dim destinoDirectorioFinal As String = Path.Combine(destino, directorioRelativo)

            carpetaPdfDestino = Path.Combine(destinoDirectorioFinal, nombreSinExt)

            ' Registrar archivo en progreso para limpieza en caso de corte abrupto
            ArchivosEnProceso.TryAdd(archivo, carpetaPdfDestino)

            Dim anioEncontrado As String = Nothing
            Dim matchAnio = System.Text.RegularExpressions.Regex.Match(nombreSinExt, "\b(19\d{2}|20\d{2})\b")
            If matchAnio.Success Then
                anioEncontrado = matchAnio.Value
            End If

            ' CONVERSIÓN: EXCLUSIVAMENTE CON MUPDF A TIFF (250 DPI LZW, ESCALA DE GRISES)
            Dim rutaMu = ModuloRecursos.ObtenerRutaEjecutableMuPdf()
            If String.IsNullOrEmpty(rutaMu) OrElse Not File.Exists(rutaMu) Then
                ModuloRecursos.AsegurarMuPdfInstalado()
                rutaMu = ModuloRecursos.ObtenerRutaEjecutableMuPdf(True)
            End If

            If String.IsNullOrEmpty(rutaMu) OrElse Not File.Exists(rutaMu) Then
                Throw New InvalidOperationException("El motor MuPDF (mutool.exe) no está instalado y no se pudo obtener automáticamente.")
            End If

            Dim maxIntentos As Integer = 3
            Dim ultimoErrorDetalle As String = ""

            For intento As Integer = 1 To maxIntentos
                If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
                    LimpiarCarpetaIncompleta(carpetaPdfDestino)
                    Return "CANCELADO"
                End If

                Try
                    If intento > 1 Then
                        ModuloConversionTiff.VaciarArchivosDeCarpeta(carpetaPdfDestino)
                    End If

                    paginas = 0
                    ConvertirPdfTiffMuPdf(archivo, carpetaPdfDestino, nombreSinExt, paginas, cancelToken, procesarPaginasEnParalelo, hilosParalelos)
                    totalPaginas = paginas

                    ' Verificar que no se haya cancelado durante la escritura
                    If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
                        LimpiarCarpetaIncompleta(carpetaPdfDestino)
                        Return "CANCELADO"
                    End If

                    ' VERIFICACIÓN FÍSICA RÁPIDA DE INTEGRIDAD Y NÚMERO DE PÁGINAS TIFF
                    Dim errorVerificacion As String = ""
                    Dim esValidoFisicamente = ValidarIntegridadFisicaTiff(carpetaPdfDestino, nombreSinExt, paginas, errorVerificacion)

                    If esValidoFisicamente AndAlso Not canceladoPorUsuario AndAlso Not cancelToken.IsCancellationRequested Then
                        estatusFinal = "OK"
                        archivoCompletadoConExito = True
                        Exit For
                    Else
                        ultimoErrorDetalle = If(Not String.IsNullOrEmpty(errorVerificacion), errorVerificacion, "Verificación física fallida")
                        archivoCompletadoConExito = False
                        ModuloConversionTiff.VaciarArchivosDeCarpeta(carpetaPdfDestino)
                    End If

                Catch exOp As OperationCanceledException
                    LimpiarCarpetaIncompleta(carpetaPdfDestino)
                    Return "CANCELADO"
                Catch exIntento As Exception
                    If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
                        LimpiarCarpetaIncompleta(carpetaPdfDestino)
                        Return "CANCELADO"
                    End If
                    Dim det As String = exIntento.Message
                    If TypeOf exIntento Is AggregateException Then
                        Dim agg = DirectCast(exIntento, AggregateException).Flatten()
                        det = String.Join(" | ", agg.InnerExceptions.Select(Function(ie) ie.GetType().Name & ": " & ie.Message))
                    ElseIf exIntento.InnerException IsNot Nothing Then
                        det &= " -> " & exIntento.InnerException.GetType().Name & ": " & exIntento.InnerException.Message
                    End If
                    ultimoErrorDetalle = det
                    archivoCompletadoConExito = False
                    ModuloConversionTiff.VaciarArchivosDeCarpeta(carpetaPdfDestino)
                End Try

                If Not archivoCompletadoConExito AndAlso intento < maxIntentos Then
                    EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | REINTENTO {intento}/{maxIntentos} | {nombreSinExt} | Motivo previo: {ultimoErrorDetalle}")
                    Thread.Sleep(500 * intento)
                End If
            Next

            If Not archivoCompletadoConExito Then
                LimpiarCarpetaIncompleta(carpetaPdfDestino)
                estatusFinal = "ERROR: " & ultimoErrorDetalle
                EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ERROR DEFINITIVO TRAS {maxIntentos} INTENTOS | {nombreSinExt} | {ultimoErrorDetalle}")
            End If

            ' SÓLO SI EL ARCHIVO ESTÁ FÍSICAMENTE COMPLETO, VERIFICADO Y NO HUBO CANCELACIÓN SE REGISTRA EN BD
            If archivoCompletadoConExito AndAlso Not canceladoPorUsuario Then
                Dim delegacionArchivo As String = delegacionSeleccionada
                Dim mDel = System.Text.RegularExpressions.Regex.Match(directorioRelativo, "(ENTREGABLE\s*\d+|ENTREGA\s*\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                If mDel.Success Then
                    delegacionArchivo = System.Text.RegularExpressions.Regex.Replace(mDel.Value, "^ENTREGA\s*", "ENTREGABLE ", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                End If

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
                    delegacionArchivo,
                    usuarioActual,
                    anioEncontrado
                )

                Dim modoHilosStr = If(procesarPaginasEnParalelo, $" [{hilosParalelos} hilos]", "")
                EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {estatusFinal} | {nombreSinExt} | {pesoMB:0.1} MB ({paginas} páginas) [MuPDF Pipeline]{modoHilosStr}")
            End If

        Catch ex As OperationCanceledException
            LimpiarCarpetaIncompleta(carpetaPdfDestino)
            estatusFinal = "CANCELADO"
        Catch ex As Exception
            LimpiarCarpetaIncompleta(carpetaPdfDestino)
            If canceladoPorUsuario OrElse cancelToken.IsCancellationRequested Then
                estatusFinal = "CANCELADO"
                Return "CANCELADO"
            End If
            Dim detalleError As String = ex.Message
            If TypeOf ex Is AggregateException Then
                Dim agg = DirectCast(ex, AggregateException).Flatten()
                detalleError = String.Join(" | ", agg.InnerExceptions.Select(Function(ie) ie.GetType().Name & ": " & ie.Message))
            ElseIf ex.InnerException IsNot Nothing Then
                detalleError &= " -> " & ex.InnerException.GetType().Name & ": " & ex.InnerException.Message
            End If
            estatusFinal = "ERROR: " & detalleError
            EscribirLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ERROR | {nombreSinExt} | {detalleError}")
        Finally
            Dim dummy As String = Nothing
            ArchivosEnProceso.TryRemove(archivo, dummy)
        End Try

        Return estatusFinal
    End Function

    ''' <summary>
    ''' Motor de conversión a TIFF basado en MuPDF (mutool.exe) y Magick.NET.
    ''' </summary>
    Private Sub ConvertirPdfTiffMuPdf(
        pdfPath As String,
        carpetaDestino As String,
        nombreBase As String,
        ByRef totalPaginas As Integer,
        token As CancellationToken,
        Optional procesarPaginasEnParalelo As Boolean = False,
        Optional hilosParalelos As Integer = 1
    )
        Dim callbackProgreso As Action(Of Integer, String) = Nothing
        Dim pesoMB As Double = 0
        Try
            pesoMB = (New FileInfo(pdfPath).Length / (1024.0 * 1024.0))
        Catch
        End Try
        If bgWorker IsNot Nothing AndAlso bgWorker.WorkerReportsProgress Then
            callbackProgreso = Sub(pct, texto)
                                   bgWorker.ReportProgress(pct, $"STATUS|{texto} ({pesoMB:0.1} MB)")
                               End Sub
        End If

        ModuloConversionTiff.ConvertirPdfTiffMuPdf(
            pdfPath,
            carpetaDestino,
            nombreBase,
            totalPaginas,
            token,
            procesarPaginasEnParalelo,
            hilosParalelos,
            callbackProgreso,
            ProcesosActivos
        )
    End Sub

    Private Function ObtenerTotalPaginasPdf(rutaPdf As String) As Integer
        Return ModuloConversionTiff.ObtenerTotalPaginasPdf(rutaPdf)
    End Function

    ''' <summary>
    ''' Verifica de forma rápida y física si un archivo PDF ya fue transferido y completado al 100% en el destino en TIFF a 250 DPI.
    ''' </summary>
    Private Function EstaPdfYaProcesadoYCompleto(
        origenBase As String,
        destinoBase As String,
        archivoPdf As String,
        ByRef totalPaginas As Integer,
        Optional cacheDirectoriosDestino As ConcurrentDictionary(Of String, Lazy(Of HashSet(Of String))) = Nothing,
        Optional mapaIndice As Dictionary(Of String, ModuloIndiceOrigen.InfoIndicePdf) = Nothing,
        Optional mapaBd As Dictionary(Of String, ModuloConversionTiff.InfoRegistroBd) = Nothing
    ) As Boolean
        Return ModuloConversionTiff.EstaPdfYaProcesadoYCompletoTiff(origenBase, destinoBase, archivoPdf, totalPaginas, cacheDirectoriosDestino, mapaIndice, mapaBd)
    End Function

    ''' <summary>
    ''' Realiza una verificación física rápida de integridad para archivos TIFF.
    ''' </summary>
    Private Function ValidarIntegridadFisicaTiff(carpetaDestino As String, nombreBase As String, totalPaginasEsperadas As Integer, ByRef mensajeError As String) As Boolean
        Return ModuloConversionTiff.ValidarIntegridadDiagnosticoRapidoTiff(carpetaDestino, nombreBase, totalPaginasEsperadas, mensajeError)
    End Function

    Private Sub LimpiarCarpetaIncompleta(carpeta As String)
        ModuloConversionTiff.LimpiarCarpetaIncompleta(carpeta)
    End Sub

    Private Sub LimpiarTodosLosIncompletosEnVuelo()
        For Each kvp In ArchivosEnProceso
            LimpiarCarpetaIncompleta(kvp.Value)
        Next
        ArchivosEnProceso.Clear()
    End Sub

    Private Function ObtenerRutaRelativa(basePath As String, fullPath As String) As String
        Return ModuloConversionTiff.ObtenerRutaRelativa(basePath, fullPath)
    End Function

    ''' <summary>
    ''' Resuelve la ruta de destino efectiva asegurando la jerarquía oficial de entregables:
    ''' Si el origen es 'ENTREGA N' (o 'ENTREGABLE N'), garantiza que el destino contenga 'ENTREGABLE N'
    ''' sin duplicar carpetas si el usuario ya la seleccionó manualmente en el destino.
    ''' </summary>
    Private Function ResolverDestinoEfectivo(origen As String, destino As String) As String
        Return ModuloConversionTiff.ResolverDestinoEfectivo(origen, destino)
    End Function

    ''' <summary>
    ''' Normaliza cualquier nivel de carpeta 'ENTREGA X' en la ruta relativa a la convención oficial 'ENTREGABLE X'.
    ''' </summary>
    Private Function NormalizarRutaRelativa(directorioRelativo As String) As String
        Return ModuloConversionTiff.NormalizarRutaRelativa(directorioRelativo)
    End Function

    Private Sub bgWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles bgWorker.ProgressChanged
        If e.UserState IsNot Nothing Then
            Dim str = e.UserState.ToString()
            If str.StartsWith("STATUS|") Then
                Me.Text = "Transferir a TIFF (250 DPI) — " & str.Substring(7)
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
            Dim rutaLog As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StellumLogs")
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