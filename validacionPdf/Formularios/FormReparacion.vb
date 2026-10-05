Imports System.Collections.Concurrent
Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class FormReparacion

    Private cancelSource As CancellationTokenSource = Nothing
    Private estaEjecutando As Boolean = False
    Private listaCompletaItems As New List(Of ItemDiagnostico)()
    Private ReadOnly ProcesosConversion As New ConcurrentDictionary(Of Integer, Process)()

    Public Class ItemDiagnostico
        Public Property Seleccionado As Boolean = True
        Public Property Estado As String = ""
        Public Property Documento As String = ""
        Public Property PagsEsperadas As Integer = 0
        Public Property JpgsFisicos As Integer = 0
        Public Property EnBd As String = "No"
        Public Property CarpetaDestino As String = ""
        Public Property CarpetaOrigen As String = ""
        Public Property RutaPdf As String = ""
        Public Property Detalle As String = ""
        Public Property EsIncompletoOFaltante As Boolean = True
    End Class

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(rutaOrigenInicial As String, rutaDestinoInicial As String)
        InitializeComponent()
        If Not String.IsNullOrEmpty(rutaOrigenInicial) Then txtOrigen.Text = rutaOrigenInicial
        If Not String.IsNullOrEmpty(rutaDestinoInicial) Then txtDestino.Text = rutaDestinoInicial
    End Sub

    Private Sub FormReparacion_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        VerificarConexionBd()
    End Sub

    Private Sub VerificarConexionBd()
        Task.Run(Sub()
                     Dim bdOk = ModuloConversionJpg.ProbarConexionMysql()
                     Me.BeginInvoke(Sub()
                                        If bdOk Then
                                            lblEstadoBd.Text = "Base de Datos: Conectada (stellumdb)"
                                            lblEstadoBd.ForeColor = Color.FromArgb(40, 120, 40)
                                        Else
                                            lblEstadoBd.Text = "Base de Datos: Sin conexión local (Modo Físico Estricto)"
                                            lblEstadoBd.ForeColor = Color.FromArgb(180, 80, 20)
                                        End If
                                    End Sub)
                 End Sub)
    End Sub

    ' ===== Navegación y Apertura Rápida de Carpetas =====

    Private Sub btnExaminarOrigen_Click(sender As Object, e As EventArgs) Handles btnExaminarOrigen.Click
        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Seleccione la carpeta con los archivos PDF de origen"
            If Directory.Exists(txtOrigen.Text) Then fbd.SelectedPath = txtOrigen.Text
            If fbd.ShowDialog(Me) = DialogResult.OK Then
                txtOrigen.Text = fbd.SelectedPath
            End If
        End Using
    End Sub

    Private Sub btnExaminarDestino_Click(sender As Object, e As EventArgs) Handles btnExaminarDestino.Click
        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Seleccione la carpeta raíz de destino"
            If Directory.Exists(txtDestino.Text) Then fbd.SelectedPath = txtDestino.Text
            If fbd.ShowDialog(Me) = DialogResult.OK Then
                txtDestino.Text = fbd.SelectedPath
            End If
        End Using
    End Sub

    Private Sub btnAbrirOrigen_Click(sender As Object, e As EventArgs) Handles btnAbrirOrigen.Click
        AbrirCarpetaEnExplorador(txtOrigen.Text)
    End Sub

    Private Sub btnAbrirDestino_Click(sender As Object, e As EventArgs) Handles btnAbrirDestino.Click
        AbrirCarpetaEnExplorador(txtDestino.Text)
    End Sub

    Private Sub AbrirCarpetaEnExplorador(ruta As String)
        Try
            If String.IsNullOrWhiteSpace(ruta) Then
                MessageBox.Show("La ruta está vacía.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            If Directory.Exists(ruta) Then
                Process.Start("explorer.exe", $"""{ruta}""")
            ElseIf File.Exists(ruta) Then
                Process.Start("explorer.exe", $"/select,""{ruta}""")
            Else
                MessageBox.Show($"La carpeta o archivo no existe en el disco:{vbCrLf}{ruta}", "No encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            MessageBox.Show("Error al abrir en explorador: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ===== Fase 1: Análisis y Detección de Incompletos =====

    Private Async Sub btnAnalizar_Click(sender As Object, e As EventArgs) Handles btnAnalizar.Click
        If estaEjecutando Then Exit Sub

        Dim origen = txtOrigen.Text.Trim()
        Dim destino = txtDestino.Text.Trim()

        If String.IsNullOrEmpty(origen) OrElse Not Directory.Exists(origen) Then
            MessageBox.Show("Seleccione una carpeta de origen válida.", "Origen Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If

        If String.IsNullOrEmpty(destino) Then
            MessageBox.Show("Seleccione una carpeta de destino.", "Destino Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If

        Dim destinoEfectivo = ModuloConversionJpg.ResolverDestinoEfectivo(origen, destino)

        ConfigurarUiEstado(True, "Analizando archivos y comparando con destino y base de datos...")
        listaCompletaItems.Clear()
        dgvArchivos.Rows.Clear()
        progressBar1.Value = 0

        cancelSource = New CancellationTokenSource()
        Dim token = cancelSource.Token

        Dim totalAnalizados As Integer = 0
        Dim totalIncompletos As Integer = 0
        Dim totalFaltantes As Integer = 0
        Dim totalCorrectos As Integer = 0

        Try
            Await Task.Run(Sub()
                               ' 1. Obtener todos los archivos PDF en origen
                               Dim archivosPdf = Directory.EnumerateFiles(origen, "*.pdf", SearchOption.AllDirectories).ToArray()
                               Dim total = archivosPdf.Length

                               If total = 0 Then
                                   Me.BeginInvoke(Sub()
                                                      lblStatus.Text = "No se encontraron archivos PDF en la carpeta de origen."
                                                      ConfigurarUiEstado(False)
                                                  End Sub)
                                   Exit Sub
                               End If

                               Dim contadorProgreso As Integer = 0

                               For Each rutaPdf In archivosPdf
                                   If token.IsCancellationRequested Then Exit For

                                   Dim item = AnalizarUnPdf(origen, destinoEfectivo, rutaPdf)
                                   listaCompletaItems.Add(item)

                                   If item.Estado.StartsWith("⚠️") OrElse item.Estado.StartsWith("📁") Then
                                       Interlocked.Increment(totalIncompletos)
                                   ElseIf item.Estado.StartsWith("❌") Then
                                       Interlocked.Increment(totalFaltantes)
                                   Else
                                       Interlocked.Increment(totalCorrectos)
                                   End If

                                   Dim actual = Interlocked.Increment(contadorProgreso)

                                   If actual Mod 20 = 0 OrElse actual = total Then
                                       Dim pct = CInt((actual / CDbl(total)) * 100)
                                       Me.BeginInvoke(Sub()
                                                          progressBar1.Value = Math.Min(100, pct)
                                                          lblStatus.Text = $"Analizando: {actual:#,##0} de {total:#,##0} ({Path.GetFileName(rutaPdf)})"
                                                          lblEstadisticas.Text = $"Analizados: {actual:#,##0}/{total:#,##0} | Correctos: {totalCorrectos:#,##0} | Incompletos: {totalIncompletos:#,##0} | Faltantes: {totalFaltantes:#,##0}"
                                                      End Sub)
                                   End If
                               Next
                           End Sub, token)

            ActualizarVistaGrilla()
            lblStatus.Text = $"Análisis finalizado. {totalIncompletos} incompletos y {totalFaltantes} faltantes detectados."
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

    Private Function AnalizarUnPdf(origenBase As String, destinoBase As String, rutaPdf As String) As ItemDiagnostico
        Dim item As New ItemDiagnostico()
        item.RutaPdf = rutaPdf
        item.Documento = Path.GetFileName(rutaPdf)
        Dim nombreSinExt = Path.GetFileNameWithoutExtension(rutaPdf)

        Dim rutaRelativa = ModuloConversionJpg.ObtenerRutaRelativa(origenBase, rutaPdf)
        Dim dirRelativo = ModuloConversionJpg.NormalizarRutaRelativa(Path.GetDirectoryName(rutaRelativa))
        Dim destinoFinal = Path.Combine(destinoBase, dirRelativo)
        Dim carpetaDestinoEsperada = Path.Combine(destinoFinal, nombreSinExt)

        item.CarpetaOrigen = Path.GetDirectoryName(rutaPdf)
        item.CarpetaDestino = carpetaDestinoEsperada

        ' 1. Obtener páginas esperadas del PDF
        Dim pagsEsperadas = ModuloConversionJpg.ObtenerTotalPaginasPdf(rutaPdf)
        item.PagsEsperadas = pagsEsperadas

        If pagsEsperadas <= 0 Then
            item.Estado = "❌ PDF no legible o corrupto"
            item.Detalle = "No se pudieron leer las páginas del PDF origen"
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        ' 2. Consultar Base de Datos
        Dim infoBd = ModuloConversionJpg.ConsultarRegistroEnBd(nombreSinExt)
        If infoBd.Encontrado Then
            item.EnBd = If(infoBd.Estatus = "OK", "Sí (OK)", $"Error ({infoBd.Estatus})")
        Else
            item.EnBd = "No"
        End If

        ' 3. Inspeccionar disco físico en destino
        If Not Directory.Exists(carpetaDestinoEsperada) Then
            item.Estado = "❌ Faltante total"
            item.JpgsFisicos = 0
            item.Detalle = "La carpeta destino no existe físicamente en el almacenamiento."
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        Dim archivosJpg = Directory.GetFiles(carpetaDestinoEsperada, "*.jpg")
        item.JpgsFisicos = archivosJpg.Length

        If archivosJpg.Length = 0 Then
            item.Estado = "📁 Carpeta vacía"
            item.Detalle = "La carpeta existe pero está vacía (0 imágenes JPG generadas)."
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        If archivosJpg.Length < pagsEsperadas Then
            Dim faltan = pagsEsperadas - archivosJpg.Length
            item.Estado = $"⚠️ Incompleto (faltan {faltan} págs)"
            item.Detalle = $"Se esperaban {pagsEsperadas} JPGs pero solo hay {archivosJpg.Length}."
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        ' Si la cantidad es igual o mayor, validar integridad estricta
        Dim errorIntegridad As String = ""
        Dim validoFisicamente = ModuloConversionJpg.ValidarIntegridadFisicaJpg(carpetaDestinoEsperada, nombreSinExt, pagsEsperadas, errorIntegridad)

        If Not validoFisicamente Then
            item.Estado = "⚠️ Incompleto (Daño)"
            item.Detalle = errorIntegridad
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            Return item
        End If

        ' Físicamente está completo. Verificar concordancia con la BD
        If infoBd.Encontrado AndAlso infoBd.Estatus = "OK" Then
            item.Estado = "✔️ Correcto (OK)"
            item.Detalle = "Físico completo y registrado en BD con estatus OK."
            item.EsIncompletoOFaltante = False
            item.Seleccionado = False
        Else
            item.Estado = "⚠️ Físico OK (Falta en BD)"
            item.Detalle = "Imágenes JPG completas en disco pero falta confirmar en BD."
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
        End If

        Return item
    End Function

    Private Sub ActualizarVistaGrilla()
        dgvArchivos.Rows.Clear()
        Dim filtrar = chkSoloIncompletos.Checked

        For Each item In listaCompletaItems
            If filtrar AndAlso Not item.EsIncompletoOFaltante Then Continue For

            Dim idx = dgvArchivos.Rows.Add(
                item.Seleccionado,
                item.Estado,
                item.Documento,
                item.PagsEsperadas,
                item.JpgsFisicos,
                item.EnBd,
                item.CarpetaDestino,
                item.CarpetaOrigen,
                item.Detalle
            )

            Dim fila = dgvArchivos.Rows(idx)
            fila.Tag = item

            ' Coloreado semántico
            If item.Estado.StartsWith("✔️") Then
                fila.Cells(ColEstado.Index).Style.BackColor = Color.FromArgb(220, 245, 220)
                fila.Cells(ColEstado.Index).Style.ForeColor = Color.FromArgb(20, 100, 20)
            ElseIf item.Estado.StartsWith("⚠️") Then
                fila.Cells(ColEstado.Index).Style.BackColor = Color.FromArgb(255, 240, 210)
                fila.Cells(ColEstado.Index).Style.ForeColor = Color.FromArgb(160, 90, 10)
            ElseIf item.Estado.StartsWith("❌") OrElse item.Estado.StartsWith("📁") Then
                fila.Cells(ColEstado.Index).Style.BackColor = Color.FromArgb(255, 225, 225)
                fila.Cells(ColEstado.Index).Style.ForeColor = Color.FromArgb(180, 20, 20)
            End If
        Next
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
            "2. Convertirá directamente los PDFs originales a imágenes JPG a 200 DPI." & vbCrLf &
            "3. Validará la integridad física de cada página generada." & vbCrLf &
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
                                   ModuloConversionJpg.LimpiarCarpetaIncompleta(carpetaDestino)

                                   ' 2. Procesar y convertir a JPG 200 DPI
                                   Dim totalPags As Integer = 0
                                   Dim exito As Boolean = False
                                   Dim msgError As String = ""

                                   Try
                                       ModuloConversionJpg.ConvertirPdfJpgMuPdf(
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
                                       If ModuloConversionJpg.ValidarIntegridadFisicaJpg(carpetaDestino, nombreSinExt, totalPags, msgError) Then
                                           exito = True
                                       Else
                                           exito = False
                                           ModuloConversionJpg.LimpiarCarpetaIncompleta(carpetaDestino)
                                       End If

                                   Catch exOp As OperationCanceledException
                                       ModuloConversionJpg.LimpiarCarpetaIncompleta(carpetaDestino)
                                       Exit Sub
                                   Catch exGen As Exception
                                       exito = False
                                       msgError = exGen.Message
                                       ModuloConversionJpg.LimpiarCarpetaIncompleta(carpetaDestino)
                                   End Try

                                   ' 4. Registro en BD si fue exitoso
                                   If exito Then
                                       Dim usuario = If(ModuloUsuarios.UsuarioActual IsNot Nothing, ModuloUsuarios.UsuarioActual.NombreUsuario, Environment.UserName)
                                       Dim anioMatch = System.Text.RegularExpressions.Regex.Match(nombreSinExt, "\b(19\d{2}|20\d{2})\b")
                                       Dim anioStr = If(anioMatch.Success, anioMatch.Value, "")
                                       Dim delegacion = Path.GetFileName(Path.GetDirectoryName(carpetaDestino))

                                       ModuloConversionJpg.RegistrarExitoEnBd(
                                           ModuloConversionJpg.ObtenerRutaRelativa(txtOrigen.Text, item.RutaPdf),
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
                                       item.Detalle = $"Reparación exitosa: {totalPags} páginas JPG (200 DPI) generadas y registradas en BD."
                                       item.PagsEsperadas = totalPags
                                       item.JpgsFisicos = totalPags
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
            btnDetener.Enabled = False
            btnDetener.Text = "Deteniendo..."
            If cancelSource IsNot Nothing Then cancelSource.Cancel()

            ' Matar procesos MuPDF activos
            For Each kvp In ProcesosConversion
                Try
                    If Not kvp.Value.HasExited Then kvp.Value.Kill()
                Catch
                End Try
            Next
            ProcesosConversion.Clear()
        End If
    End Sub

    Private Sub ConfigurarUiEstado(ejecutando As Boolean, Optional mensaje As String = "")
        estaEjecutando = ejecutando
        btnAnalizar.Enabled = Not ejecutando
        btnReparar.Enabled = Not ejecutando AndAlso listaCompletaItems.Any(Function(i) i.EsIncompletoOFaltante)
        btnDetener.Enabled = ejecutando
        btnDetener.Text = "⏹ Detener"
        btnExaminarOrigen.Enabled = Not ejecutando
        btnExaminarDestino.Enabled = Not ejecutando
        txtOrigen.ReadOnly = ejecutando
        txtDestino.ReadOnly = ejecutando
        If Not String.IsNullOrEmpty(mensaje) Then lblStatus.Text = mensaje
    End Sub

    ' ===== Menú Contextual y Doble Clic =====

    Private Sub dgvArchivos_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvArchivos.CellDoubleClick
        If e.RowIndex < 0 Then Exit Sub
        Dim row = dgvArchivos.Rows(e.RowIndex)
        Dim item = TryCast(row.Tag, ItemDiagnostico)
        If item IsNot Nothing Then
            ' Abrir la carpeta destino si existe, si no abrir la de origen
            If Directory.Exists(item.CarpetaDestino) Then
                AbrirCarpetaEnExplorador(item.CarpetaDestino)
            Else
                AbrirCarpetaEnExplorador(item.CarpetaOrigen)
            End If
        End If
    End Sub

    Private Sub menuItemAbrirDestino_Click(sender As Object, e As EventArgs) Handles menuItemAbrirDestino.Click
        If dgvArchivos.SelectedRows.Count = 0 Then Exit Sub
        Dim item = TryCast(dgvArchivos.SelectedRows(0).Tag, ItemDiagnostico)
        If item IsNot Nothing Then
            AbrirCarpetaEnExplorador(item.CarpetaDestino)
        End If
    End Sub

    Private Sub menuItemAbrirOrigen_Click(sender As Object, e As EventArgs) Handles menuItemAbrirOrigen.Click
        If dgvArchivos.SelectedRows.Count = 0 Then Exit Sub
        Dim item = TryCast(dgvArchivos.SelectedRows(0).Tag, ItemDiagnostico)
        If item IsNot Nothing Then
            AbrirCarpetaEnExplorador(item.CarpetaOrigen)
        End If
    End Sub

    Private Sub menuItemAbrirPdf_Click(sender As Object, e As EventArgs) Handles menuItemAbrirPdf.Click
        If dgvArchivos.SelectedRows.Count = 0 Then Exit Sub
        Dim item = TryCast(dgvArchivos.SelectedRows(0).Tag, ItemDiagnostico)
        If item IsNot Nothing AndAlso File.Exists(item.RutaPdf) Then
            Try
                Process.Start(item.RutaPdf)
            Catch ex As Exception
                MessageBox.Show("Error al abrir PDF: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    Private Sub menuItemRepararUno_Click(sender As Object, e As EventArgs) Handles menuItemRepararUno.Click
        If dgvArchivos.SelectedRows.Count = 0 OrElse estaEjecutando Then Exit Sub
        Dim item = TryCast(dgvArchivos.SelectedRows(0).Tag, ItemDiagnostico)
        If item Is Nothing Then Exit Sub

        For Each it In listaCompletaItems
            it.Seleccionado = False
        Next
        item.Seleccionado = True
        ActualizarVistaGrilla()
        btnReparar.PerformClick()
    End Sub

    Private Sub FormReparacion_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If estaEjecutando Then
            Dim resp = MessageBox.Show(
                "Hay una operación en curso. ¿Desea cancelar y salir?",
                "Operación en Progreso",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
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
