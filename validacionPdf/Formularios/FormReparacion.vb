Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Concurrent
Imports System.Windows.Forms
Imports System.Drawing

Public Class FormReparacion

    Private cancelSource As CancellationTokenSource = Nothing
    Private estaEjecutando As Boolean = False
    Private listaCompletaItems As New List(Of ItemDiagnostico)()
    Private ProcesosConversion As New ConcurrentDictionary(Of Integer, Process)()

    Public Class ItemDiagnostico
        Public Property Seleccionado As Boolean = False
        Public Property Estado As String = ""
        Public Property Documento As String = ""
        Public Property PagsEsperadas As Integer = 0
        Public Property TiffsFisicos As Integer = 0
        Public Property EnBd As String = "No"
        Public Property CarpetaDestino As String = ""
        Public Property CarpetaOrigen As String = ""
        Public Property Detalle As String = ""
        Public Property RutaPdf As String = ""
        Public Property EsIncompletoOFaltante As Boolean = False
    End Class

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(rutaOrigenInicial As String, rutaDestinoInicial As String)
        Me.New()
        If Not String.IsNullOrEmpty(rutaOrigenInicial) Then txtOrigen.Text = rutaOrigenInicial
        If Not String.IsNullOrEmpty(rutaDestinoInicial) Then txtDestino.Text = rutaDestinoInicial
    End Sub

    Private Sub FormReparacion_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ConfigurarEstilosVisuales()
        lblStatus.Text = "Listo para iniciar auditoría y reparación de archivos TIFF."
        ActualizarEstadoBdIndicador()
    End Sub

    Private Sub ConfigurarEstilosVisuales()
        dgvArchivos.AutoGenerateColumns = False
        dgvArchivos.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvArchivos.MultiSelect = True
        dgvArchivos.DoubleBuffered(True)
    End Sub

    Private Sub ActualizarEstadoBdIndicador()
        Task.Run(Sub()
                     Try
                         Using conn As New MySqlConnector.MySqlConnection(ModuloConversionTiff.CadenaConexionMysql)
                             conn.Open()
                             Dim cmd As New MySqlConnector.MySqlCommand($"SELECT COUNT(*) FROM {ModuloConversionTiff.NombreTablaBitacora}", conn)
                             Dim total = Convert.ToInt64(cmd.ExecuteScalar())
                             Me.BeginInvoke(Sub()
                                                lblEstadoBd.Text = $"BD Conectada ({ModuloConversionTiff.NombreTablaBitacora}): {total:#,##0} registros"
                                                lblEstadoBd.ForeColor = Color.DarkGreen
                                            End Sub)
                         End Using
                     Catch ex As Exception
                         Me.BeginInvoke(Sub()
                                            lblEstadoBd.Text = "BD: Desconectada o error de acceso"
                                            lblEstadoBd.ForeColor = Color.Firebrick
                                        End Sub)
                     End Try
                 End Sub)
    End Sub

    Private Sub btnExaminarOrigen_Click(sender As Object, e As EventArgs) Handles btnExaminarOrigen.Click
        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Seleccione la carpeta de origen con los archivos PDF"
            If Directory.Exists(txtOrigen.Text) Then fbd.SelectedPath = txtOrigen.Text
            If fbd.ShowDialog(Me) = DialogResult.OK Then
                txtOrigen.Text = fbd.SelectedPath
            End If
        End Using
    End Sub

    Private Sub btnExaminarDestino_Click(sender As Object, e As EventArgs) Handles btnExaminarDestino.Click
        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Seleccione la carpeta raíz de destino para las imágenes TIFF"
            If Directory.Exists(txtDestino.Text) Then fbd.SelectedPath = txtDestino.Text
            If fbd.ShowDialog(Me) = DialogResult.OK Then
                txtDestino.Text = fbd.SelectedPath
            End If
        End Using
    End Sub

    Private Sub btnAbrirOrigen_Click(sender As Object, e As EventArgs) Handles btnAbrirOrigen.Click
        AbrirEnExplorador(txtOrigen.Text)
    End Sub

    Private Sub btnAbrirDestino_Click(sender As Object, e As EventArgs) Handles btnAbrirDestino.Click
        AbrirEnExplorador(txtDestino.Text)
    End Sub

    Private Sub AbrirEnExplorador(ruta As String)
        Try
            If Directory.Exists(ruta) Then
                Process.Start("explorer.exe", ruta)
            Else
                MessageBox.Show("La ruta indicada no existe en el sistema.", "Carpeta no encontrada", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            MessageBox.Show("Error al abrir explorador: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ===== Fase 1: Análisis y Diagnóstico Masivo =====

    Private Async Sub btnAnalizar_Click(sender As Object, e As EventArgs) Handles btnAnalizar.Click
        If estaEjecutando Then Exit Sub

        Dim origen = txtOrigen.Text.Trim()
        Dim destino = txtDestino.Text.Trim()

        If Not Directory.Exists(origen) Then
            MessageBox.Show("La carpeta de origen de PDFs no existe o no es accesible.", "Origen inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If String.IsNullOrWhiteSpace(destino) Then
            MessageBox.Show("Debe especificar la carpeta raíz de destino.", "Destino requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Si la carpeta destino seleccionada ya contiene la delegación, ajustarla
        Dim delegacionOrigen = Path.GetFileName(origen.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        Dim destinoEfectivo = destino
        If Not destinoEfectivo.TrimEnd(Path.DirectorySeparatorChar).EndsWith(delegacionOrigen, StringComparison.OrdinalIgnoreCase) Then
            Dim subDirPosible = Path.Combine(destinoEfectivo, delegacionOrigen)
            If Directory.Exists(subDirPosible) Then
                destinoEfectivo = subDirPosible
            End If
        End If

        ConfigurarUiEstado(True, "Iniciando escaneo de archivos PDF...")
        cancelSource = New CancellationTokenSource()
        Dim token = cancelSource.Token

        listaCompletaItems.Clear()
        dgvArchivos.Rows.Clear()
        progressBar1.Value = 0

        Dim totalIncompletos As Integer = 0
        Dim totalFaltantes As Integer = 0
        Dim totalCorrectos As Integer = 0

        Try
            Await Task.Run(Sub()
                               ' 1. Búsqueda exhaustiva recursiva de PDFs en origen
                               Me.BeginInvoke(Sub() lblStatus.Text = "Buscando archivos PDF en la carpeta origen...")
                               Dim archivosPdf = Directory.GetFiles(origen, "*.pdf", SearchOption.AllDirectories)
                               Dim total = archivosPdf.Length

                               If total = 0 Then
                                   Me.BeginInvoke(Sub()
                                                      lblStatus.Text = "No se encontraron archivos PDF en el origen seleccionado."
                                                      MessageBox.Show("No se encontraron archivos PDF en la ruta de origen especificada.", "Sin archivos", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                  End Sub)
                                   Exit Sub
                               End If

                               ' 2. Cargar la base de datos completa en memoria en UN SOLO VIAJE (O(1))
                               Me.BeginInvoke(Sub() lblStatus.Text = "Cargando índice de base de datos TIFF en memoria para máxima velocidad...")
                               Dim mapaBd = ModuloConversionTiff.CargarDiccionarioBitacoraEnMemoria()

                               Dim contadorProgreso As Integer = 0
                               Dim swRelojUI = Diagnostics.Stopwatch.StartNew()
                               Dim bagItems As New ConcurrentBag(Of ItemDiagnostico)()

                               ' 3. Análisis masivo multinúcleo concurrente usando la potencia óptima detectada por el sistema
                               Dim numHilos = Math.Max(4, ModuloRecursos.ObtenerHilosOptimos())
                               Dim popt As New ParallelOptions With {
                                   .MaxDegreeOfParallelism = numHilos,
                                   .CancellationToken = token
                               }

                               Parallel.ForEach(archivosPdf, popt, Sub(rutaPdf, loopState)
                                   If token.IsCancellationRequested Then
                                       loopState.Stop()
                                       Exit Sub
                                   End If

                                   Dim item = AnalizarUnPdfOptimizado(origen, destinoEfectivo, rutaPdf, mapaBd)
                                   bagItems.Add(item)

                                   If item.Estado.StartsWith("⚠️") OrElse item.Estado.StartsWith("📁") Then
                                       Interlocked.Increment(totalIncompletos)
                                   ElseIf item.Estado.StartsWith("❌") Then
                                       Interlocked.Increment(totalFaltantes)
                                   Else
                                       Interlocked.Increment(totalCorrectos)
                                   End If

                                   Dim actual = Interlocked.Increment(contadorProgreso)

                                   ' Actualizar interfaz por lotes en tiempo real sin saturar el hilo principal
                                   If swRelojUI.ElapsedMilliseconds >= 250 OrElse actual = total Then
                                       SyncLock swRelojUI
                                           If swRelojUI.ElapsedMilliseconds >= 250 OrElse actual = total Then
                                               swRelojUI.Restart()
                                               Dim pct = CInt((actual / CDbl(total)) * 100)
                                               Me.BeginInvoke(Sub()
                                                                  progressBar1.Value = Math.Min(100, pct)
                                                                  lblStatus.Text = $"Analizando a alta velocidad ({numHilos} núcleos): {actual:#,##0} de {total:#,##0}..."
                                                                  lblEstadisticas.Text = $"Analizados: {actual:#,##0}/{total:#,##0} | Correctos: {totalCorrectos:#,##0} | Incompletos: {totalIncompletos:#,##0} | Faltantes: {totalFaltantes:#,##0}"
                                                              End Sub)
                                           End If
                                       End SyncLock
                                   End If
                               End Sub)

                               listaCompletaItems = bagItems.OrderBy(Function(i) i.Documento).ToList()
                           End Sub, token)

            ActualizarVistaGrilla()
            lblStatus.Text = $"Análisis finalizado a máxima velocidad. {totalIncompletos:#,##0} incompletos y {totalFaltantes:#,##0} faltantes detectados."
            lblEstadisticas.Text = $"Total: {listaCompletaItems.Count:#,##0} | Correctos: {totalCorrectos:#,##0} | Incompletos: {totalIncompletos:#,##0} | Faltantes: {totalFaltantes:#,##0}"

            btnReparar.Enabled = (totalIncompletos + totalFaltantes > 0)

        Catch ex As OperationCanceledException
            lblStatus.Text = "Análisis detenido por el usuario."
        Catch ex As Exception
            MessageBox.Show("Error durante el análisis: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            lblStatus.Text = "Error en el análisis."
        Finally
            ConfigurarUiEstado(False)
        End Try
    End Sub

    ''' <summary>
    ''' Auditoría ultrarrápida: consulta la BD en RAM (0 ms) y solo examina a fondo los que tienen discrepancias físicas.
    ''' </summary>
    Private Function AnalizarUnPdfOptimizado(
        origenBase As String,
        destinoBase As String,
        rutaPdf As String,
        mapaBd As Dictionary(Of String, ModuloConversionTiff.InfoRegistroBd)
    ) As ItemDiagnostico
        Dim item As New ItemDiagnostico()
        item.RutaPdf = rutaPdf
        item.Documento = Path.GetFileName(rutaPdf)
        Dim nombreSinExt = Path.GetFileNameWithoutExtension(rutaPdf)

        Dim rutaRelativa = ModuloConversionTiff.ObtenerRutaRelativa(origenBase, rutaPdf)
        Dim dirRelativo = ModuloConversionTiff.NormalizarRutaRelativa(Path.GetDirectoryName(rutaRelativa))
        Dim destinoFinal = Path.Combine(destinoBase, dirRelativo)
        Dim carpetaDestinoEsperada = Path.Combine(destinoFinal, nombreSinExt)

        item.CarpetaOrigen = Path.GetDirectoryName(rutaPdf)
        item.CarpetaDestino = carpetaDestinoEsperada

        ' 1. Consulta ultrarrápida en memoria (O(1))
        Dim infoBd As ModuloConversionTiff.InfoRegistroBd = Nothing
        If mapaBd IsNot Nothing Then
            If Not mapaBd.TryGetValue(nombreSinExt, infoBd) Then
                mapaBd.TryGetValue(item.Documento, infoBd)
            End If
        End If

        If infoBd IsNot Nothing AndAlso infoBd.Encontrado Then
            item.EnBd = If(infoBd.Estatus = "OK", "Sí (OK)", $"Error ({infoBd.Estatus})")
        Else
            item.EnBd = "No"
        End If

        ' 2. Inspección rápida de disco en destino
        If Not Directory.Exists(carpetaDestinoEsperada) Then
            item.Estado = "❌ Faltante total"
            item.TiffsFisicos = 0
            item.PagsEsperadas = If(infoBd IsNot Nothing AndAlso infoBd.Paginas > 0, infoBd.Paginas, 0)
            item.Detalle = "La carpeta destino no existe físicamente en el almacenamiento."
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        Dim archivosTiff = Directory.GetFiles(carpetaDestinoEsperada, "*.tiff")
        item.TiffsFisicos = archivosTiff.Length

        If archivosTiff.Length = 0 Then
            item.Estado = "📁 Carpeta vacía"
            item.PagsEsperadas = If(infoBd IsNot Nothing AndAlso infoBd.Paginas > 0, infoBd.Paginas, 0)
            item.Detalle = "La carpeta existe pero está vacía (0 imágenes TIFF generadas)."
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        ' 3. Vía rápida para los ya completados y registrados en BD (Evita leer 100,000 PDFs por red SMB)
        If infoBd IsNot Nothing AndAlso infoBd.Estatus = "OK" AndAlso infoBd.Paginas > 0 AndAlso infoBd.Paginas = archivosTiff.Length Then
            item.PagsEsperadas = infoBd.Paginas
            item.Estado = "✔️ Correcto (OK)"
            item.Detalle = "Físico completo y verificado contra registro en BD."
            item.EsIncompletoOFaltante = False
            item.Seleccionado = False
            Return item
        End If

        ' 4. Si hay discrepancia o no está en BD, examinar el PDF original con iText 7
        Dim pagsEsperadas = ModuloConversionTiff.ObtenerTotalPaginasPdf(rutaPdf)
        item.PagsEsperadas = pagsEsperadas

        If pagsEsperadas <= 0 Then
            item.Estado = "❌ PDF no legible o corrupto"
            item.Detalle = "No se pudieron leer las páginas del PDF origen"
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        If archivosTiff.Length < pagsEsperadas Then
            Dim faltan = pagsEsperadas - archivosTiff.Length
            item.Estado = $"⚠️ Incompleto (faltan {faltan} págs)"
            item.Detalle = $"Se esperaban {pagsEsperadas} TIFFs pero solo hay {archivosTiff.Length}."
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        ' 5. Validación de integridad física estricta
        Dim errorIntegridad As String = ""
        Dim validoFisicamente = ModuloConversionTiff.ValidarIntegridadFisicaTiff(carpetaDestinoEsperada, nombreSinExt, pagsEsperadas, errorIntegridad)

        If Not validoFisicamente Then
            item.Estado = "⚠️ Incompleto (Daño)"
            item.Detalle = errorIntegridad
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        ' 6. Verificación de concordancia final
        If infoBd IsNot Nothing AndAlso infoBd.Estatus = "OK" Then
            item.Estado = "✔️ Correcto (OK)"
            item.Detalle = "Físico completo y registrado en BD con estatus OK."
            item.EsIncompletoOFaltante = False
            item.Seleccionado = False
        Else
            item.Estado = "⚠️ No registrado en BD"
            item.Detalle = "Los TIFFs están completos en disco pero falta estatus OK en la Base de Datos."
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
        End If

        Return item
    End Function

    Private Sub ActualizarVistaGrilla()
        dgvArchivos.SuspendLayout()
        dgvArchivos.Rows.Clear()
        Dim filtrar = chkSoloIncompletos.Checked

        Dim itemsMostrar = If(filtrar, listaCompletaItems.Where(Function(it) it.EsIncompletoOFaltante).ToList(), listaCompletaItems)
        Dim filasNuevas As New List(Of DataGridViewRow)(Math.Min(itemsMostrar.Count, 5000))

        For Each item In itemsMostrar
            Dim fila As New DataGridViewRow()
            fila.CreateCells(dgvArchivos,
                item.Seleccionado,
                item.Estado,
                item.Documento,
                item.PagsEsperadas,
                item.TiffsFisicos,
                item.EnBd,
                item.CarpetaDestino,
                item.CarpetaOrigen,
                item.Detalle
            )
            fila.Tag = item

            ' Coloreado semántico
            If item.Estado.StartsWith("✔️") Then
                fila.Cells(ColEstado.Index).Style.BackColor = Color.FromArgb(220, 245, 220)
                fila.Cells(ColEstado.Index).Style.ForeColor = Color.FromArgb(20, 120, 20)
            ElseIf item.Estado.StartsWith("⚠️") Then
                fila.Cells(ColEstado.Index).Style.BackColor = Color.FromArgb(255, 245, 210)
                fila.Cells(ColEstado.Index).Style.ForeColor = Color.FromArgb(160, 100, 0)
            ElseIf item.Estado.StartsWith("❌") OrElse item.Estado.StartsWith("📁") Then
                fila.Cells(ColEstado.Index).Style.BackColor = Color.FromArgb(255, 225, 225)
                fila.Cells(ColEstado.Index).Style.ForeColor = Color.FromArgb(180, 20, 20)
            End If

            filasNuevas.Add(fila)
        Next

        If filasNuevas.Count > 0 Then
            dgvArchivos.Rows.AddRange(filasNuevas.ToArray())
        End If

        dgvArchivos.ResumeLayout()
    End Sub

    Private Sub chkSoloIncompletos_CheckedChanged(sender As Object, e As EventArgs) Handles chkSoloIncompletos.CheckedChanged
        ActualizarVistaGrilla()
    End Sub

    ' ===== Fase 2: Reparación de Incompletos y Faltantes =====

    Private Async Sub btnReparar_Click(sender As Object, e As EventArgs) Handles btnReparar.Click
        If estaEjecutando Then Exit Sub

        ' Recoger elementos marcados para reparar
        SincronizarSeleccionDesdeGrilla()
        Dim aReparar = listaCompletaItems.Where(Function(it) it.Seleccionado AndAlso it.EsIncompletoOFaltante).ToList()

        If aReparar.Count = 0 Then
            MessageBox.Show("No hay archivos incompletos o faltantes seleccionados para reparar.", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If

        Dim resp = MessageBox.Show(
            $"Se repararán {aReparar.Count} archivos detectados con discrepancias o incompletos.{vbCrLf}{vbCrLf}" &
            "Acciones que realizará el sistema:" & vbCrLf &
            "1. Eliminará las carpetas y archivos destino incompletos para evitar mezclas o corrupción." & vbCrLf &
            "2. Convertirá directamente los PDFs originales a imágenes TIFF a 250 DPI (LZW)." & vbCrLf &
            "3. Validará la integridad física y cabeceras TIFF de cada página generada." & vbCrLf &
            "4. Actualizará la Base de Datos con estatus OK para que los números cuadren al 100%." & vbCrLf & vbCrLf &
            "¿Desea iniciar la reparación ahora?",
            "Confirmar Reparación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        )

        If resp <> DialogResult.Yes Then Exit Sub

        ConfigurarUiEstado(True, "Iniciando reparación de archivos incompletos...")
        cancelSource = New CancellationTokenSource()
        Dim token = cancelSource.Token

        Dim total = aReparar.Count
        Dim procesados As Integer = 0
        Dim reparadosOk As Integer = 0
        Dim fallados As Integer = 0

        progressBar1.Value = 0

        Dim maxHilos = Math.Max(2, Math.Min(Environment.ProcessorCount, ModuloRecursos.ObtenerHilosOptimos()))

        Try
            Await Task.Run(Sub()
                               Dim popt As New ParallelOptions With {
                                   .MaxDegreeOfParallelism = maxHilos,
                                   .CancellationToken = token
                               }

                               Parallel.ForEach(aReparar, popt, Sub(item, state)
                                   If token.IsCancellationRequested Then state.Stop() : Exit Sub

                                   Dim nombreSinExt = Path.GetFileNameWithoutExtension(item.Documento)
                                   Dim carpetaDestino = item.CarpetaDestino

                                   ' 1. Borrar carpeta incompleta si existe para iniciar limpio
                                   ModuloConversionTiff.LimpiarCarpetaIncompleta(carpetaDestino)

                                   ' 2. Procesar y convertir a TIFF 250 DPI LZW
                                   Dim totalPags As Integer = 0
                                   Dim exito As Boolean = False
                                   Dim msgError As String = ""

                                   Try
                                       ModuloConversionTiff.ConvertirPdfTiffMuPdf(
                                           item.RutaPdf,
                                           carpetaDestino,
                                           nombreSinExt,
                                           totalPags,
                                           token,
                                           False,
                                           1,
                                           Nothing,
                                           ProcesosConversion
                                       )

                                       ' 3. Validación física de integridad
                                       If ModuloConversionTiff.ValidarIntegridadFisicaTiff(carpetaDestino, nombreSinExt, totalPags, msgError) Then
                                           exito = True
                                       Else
                                           exito = False
                                           ModuloConversionTiff.LimpiarCarpetaIncompleta(carpetaDestino)
                                       End If

                                   Catch exOp As OperationCanceledException
                                       ModuloConversionTiff.LimpiarCarpetaIncompleta(carpetaDestino)
                                       Exit Sub
                                   Catch exGen As Exception
                                       exito = False
                                       msgError = exGen.Message
                                       ModuloConversionTiff.LimpiarCarpetaIncompleta(carpetaDestino)
                                   End Try

                                   ' 4. Registro en BD si fue exitoso
                                   If exito Then
                                       Dim usuario = If(ModuloUsuarios.UsuarioActual IsNot Nothing, ModuloUsuarios.UsuarioActual.NombreUsuario, Environment.UserName)
                                       Dim anioMatch = System.Text.RegularExpressions.Regex.Match(nombreSinExt, "\b(19\d{2}|20\d{2})\b")
                                       Dim anioStr = If(anioMatch.Success, anioMatch.Value, "")
                                       Dim delegacion = Path.GetFileName(Path.GetDirectoryName(carpetaDestino))

                                       ModuloConversionTiff.RegistrarExitoEnBd(
                                           ModuloConversionTiff.ObtenerRutaRelativa(txtOrigen.Text, item.RutaPdf),
                                           item.CarpetaOrigen,
                                           carpetaDestino,
                                           item.Documento,
                                           item.Documento,
                                           item.Documento,
                                           totalPags,
                                           delegacion,
                                           usuario,
                                           anioStr
                                       )

                                       item.Estado = "✔️ Reparado OK"
                                       item.Detalle = $"Reparación exitosa: {totalPags} páginas TIFF (250 DPI) generadas y registradas en BD."
                                       item.PagsEsperadas = totalPags
                                       item.TiffsFisicos = totalPags
                                       item.EnBd = "Sí (OK)"
                                       item.EsIncompletoOFaltante = False
                                       item.Seleccionado = False
                                       Interlocked.Increment(reparadosOk)
                                   Else
                                       item.Estado = "❌ Error al reparar"
                                       item.Detalle = msgError
                                       Interlocked.Increment(fallados)
                                   End If

                                   Dim actual = Interlocked.Increment(procesados)
                                   Dim pct = CInt((actual / CDbl(total)) * 100)

                                   Me.BeginInvoke(Sub()
                                                      progressBar1.Value = Math.Min(100, pct)
                                                      lblStatus.Text = $"Reparando ({actual}/{total}): {item.Documento} -> {item.Estado}"
                                                      lblEstadisticas.Text = $"Total a reparar: {total} | Reparados OK: {reparadosOk} | Errores: {fallados}"
                                                  End Sub)
                               End Sub)
                           End Sub, token)

            ActualizarVistaGrilla()
            MessageBox.Show(
                $"Reparación completada.{vbCrLf}{vbCrLf}" &
                $"✔️ Reparados con éxito: {reparadosOk}{vbCrLf}" &
                $"❌ Errores: {fallados}{vbCrLf}{vbCrLf}" &
                "Los archivos reparados han sido sincronizados físicamente y en la base de datos.",
                "Reparación Finalizada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            )

        Catch ex As OperationCanceledException
            lblStatus.Text = "Reparación detenida por el usuario."
        Catch ex As Exception
            MessageBox.Show("Error durante la reparación: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            ConfigurarUiEstado(False)
        End Try
    End Sub

    Private Sub SincronizarSeleccionDesdeGrilla()
        For Each row As DataGridViewRow In dgvArchivos.Rows
            Dim it = TryCast(row.Tag, ItemDiagnostico)
            If it IsNot Nothing Then
                Dim val = row.Cells(ColSeleccion.Index).Value
                it.Seleccionado = (val IsNot Nothing AndAlso CBool(val) = True)
            End If
        Next
    End Sub

    ' ===== Control de Cancelación y UI =====

    Private Sub btnDetener_Click(sender As Object, e As EventArgs) Handles btnDetener.Click
        If Not estaEjecutando Then Exit Sub

        Dim resp = MessageBox.Show(
            "¿Desea detener el proceso actual?" & vbCrLf &
            "Los archivos que se encuentren a la mitad serán limpiados para no dejar residuos.",
            "Confirmar Detención",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        )

        If resp = DialogResult.Yes Then
            lblStatus.Text = "Deteniendo procesos en curso..."
            If cancelSource IsNot Nothing Then cancelSource.Cancel()

            ' Matar subprocesos de mutool activos
            For Each kvp In ProcesosConversion
                Try
                    If Not kvp.Value.HasExited Then kvp.Value.Kill()
                Catch
                End Try
            Next
            ProcesosConversion.Clear()
        End If
    End Sub

    Private Sub ConfigurarUiEstado(ejecutando As Boolean, Optional textoEstado As String = "")
        estaEjecutando = ejecutando
        btnAnalizar.Enabled = Not ejecutando
        btnReparar.Enabled = Not ejecutando AndAlso listaCompletaItems.Any(Function(i) i.EsIncompletoOFaltante)
        btnDetener.Enabled = ejecutando
        btnExaminarOrigen.Enabled = Not ejecutando
        btnExaminarDestino.Enabled = Not ejecutando
        txtOrigen.ReadOnly = ejecutando
        txtDestino.ReadOnly = ejecutando

        If Not String.IsNullOrEmpty(textoEstado) Then
            lblStatus.Text = textoEstado
        End If
    End Sub

    ' ===== Context Menu y Acceso Rápido a Carpetas =====

    Private Sub dgvArchivos_CellMouseDown(sender As Object, e As DataGridViewCellMouseEventArgs) Handles dgvArchivos.CellMouseDown
        If e.Button = MouseButtons.Right AndAlso e.RowIndex >= 0 Then
            dgvArchivos.ClearSelection()
            dgvArchivos.Rows(e.RowIndex).Selected = True
            contextMenuDgv.Show(Cursor.Position)
        End If
    End Sub

    Private Sub dgvArchivos_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvArchivos.CellDoubleClick
        If e.RowIndex < 0 Then Exit Sub
        Dim item = TryCast(dgvArchivos.Rows(e.RowIndex).Tag, ItemDiagnostico)
        If item IsNot Nothing Then
            If Directory.Exists(item.CarpetaDestino) Then
                AbrirEnExplorador(item.CarpetaDestino)
            ElseIf Directory.Exists(item.CarpetaOrigen) Then
                AbrirEnExplorador(item.CarpetaOrigen)
            End If
        End If
    End Sub

    Private Sub menuItemAbrirDestino_Click(sender As Object, e As EventArgs) Handles menuItemAbrirDestino.Click
        If dgvArchivos.SelectedRows.Count = 0 Then Exit Sub
        Dim item = TryCast(dgvArchivos.SelectedRows(0).Tag, ItemDiagnostico)
        If item IsNot Nothing Then
            If Directory.Exists(item.CarpetaDestino) Then
                AbrirEnExplorador(item.CarpetaDestino)
            Else
                Dim padre = Path.GetDirectoryName(item.CarpetaDestino)
                If Directory.Exists(padre) Then
                    AbrirEnExplorador(padre)
                Else
                    MessageBox.Show("La carpeta destino aún no existe físicamente.", "No existe", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End If
        End If
    End Sub

    Private Sub menuItemAbrirOrigen_Click(sender As Object, e As EventArgs) Handles menuItemAbrirOrigen.Click
        If dgvArchivos.SelectedRows.Count = 0 Then Exit Sub
        Dim item = TryCast(dgvArchivos.SelectedRows(0).Tag, ItemDiagnostico)
        If item IsNot Nothing Then
            AbrirEnExplorador(item.CarpetaOrigen)
        End If
    End Sub

    Private Sub FormReparacion_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If estaEjecutando Then
            Dim resp = MessageBox.Show(
                "Hay una operación en segundo plano activa." & vbCrLf &
                "¿Desea detenerla y cerrar la ventana?",
                "Confirmar Salida",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            )
            If resp = DialogResult.Yes Then
                If cancelSource IsNot Nothing Then cancelSource.Cancel()
                For Each kvp In ProcesosConversion
                    Try
                        If Not kvp.Value.HasExited Then kvp.Value.Kill()
                    Catch
                    End Try
                Next
            Else
                e.Cancel = True
            End If
        End If
    End Sub

End Class

Public Module ControlExtensions
    <System.Runtime.CompilerServices.Extension()>
    Public Sub DoubleBuffered(dgv As DataGridView, setting As Boolean)
        Dim dgvType = dgv.GetType()
        Dim pi = dgvType.GetProperty("DoubleBuffered", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
        If pi IsNot Nothing Then pi.SetValue(dgv, setting, Nothing)
    End Sub
End Module
