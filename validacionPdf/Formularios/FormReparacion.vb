Imports System.Collections.Concurrent
Imports System.ComponentModel
Imports System.IO
Imports System.Text
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
        Public Property TamanoBytes As Long = 0
        Public Property IdBd As Long = 0
    End Class

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(rutaOrigenInicial As String, rutaDestinoInicial As String)
        InitializeComponent()
        If Not String.IsNullOrEmpty(rutaOrigenInicial) Then txtOrigen.Text = rutaOrigenInicial
        If Not String.IsNullOrEmpty(rutaDestinoInicial) Then txtDestino.Text = rutaDestinoInicial
    End Sub

    Private cancelSourceIndexar As CancellationTokenSource = Nothing
    Private estaIndexando As Boolean = False

    Private Sub FormReparacion_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        VerificarConexionBd()
        ActualizarResumenIndiceEnUi(txtOrigen.Text.Trim())
    End Sub

    Private Sub txtOrigen_TextChanged(sender As Object, e As EventArgs) Handles txtOrigen.TextChanged
        ActualizarResumenIndiceEnUi(txtOrigen.Text.Trim())
    End Sub

    Private Sub ActualizarResumenIndiceEnUi(ruta As String)
        If String.IsNullOrWhiteSpace(ruta) Then
            lblResumenIndice.Text = "Índice: Seleccione una carpeta de origen"
            lblResumenIndice.ForeColor = Color.FromArgb(100, 100, 100)
            Exit Sub
        End If

        lblResumenIndice.Text = "Índice: Consultando..."
        lblResumenIndice.ForeColor = Color.FromArgb(100, 100, 100)

        Task.Run(Sub()
                     Dim resumen = ModuloIndiceOrigen.ObtenerResumenIndice(ruta)
                     Me.BeginInvoke(Sub()
                                        If txtOrigen.Text.Trim().Equals(ruta.Trim(), StringComparison.OrdinalIgnoreCase) Then
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
        If estaEjecutando OrElse estaIndexando Then
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

        Dim ruta = txtOrigen.Text.Trim()
        If String.IsNullOrWhiteSpace(ruta) OrElse Not Directory.Exists(ruta) Then
            MessageBox.Show("Seleccione una carpeta de origen válida antes de indexar.", "Origen no seleccionado", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If

        Dim resumenActual = ModuloIndiceOrigen.ObtenerResumenIndice(ruta)
        Dim msgPregunta = $"Se creará/reconstruirá el índice de PDFs de origen en la base de datos para la ruta:{vbCrLf}{vbCrLf}" &
                          $"{ruta}{vbCrLf}{vbCrLf}"
        If resumenActual.Existe Then
            msgPregunta &= $"Actualmente hay {resumenActual.TotalPdfs:#,##0} PDFs y {resumenActual.TotalPaginas:#,##0} páginas indexadas en esta ubicación.{vbCrLf}" &
                           "Al continuar, se eliminará el índice anterior de esta ruta y se reindexará desde cero con los archivos actuales.{vbCrLf}{vbCrLf}"
        Else
            msgPregunta &= "Esta ubicación no tiene índice registrado todavía.{vbCrLf}{vbCrLf}"
        End If
        msgPregunta &= "¿Desea iniciar la indexación ahora?"

        If MessageBox.Show(msgPregunta, "Confirmar Indexado de Origen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Exit Sub
        End If

        estaIndexando = True
        ConfigurarUiEstado(True, "Iniciando indexación de PDFs de origen...")
        btnIndexarOrigen.Enabled = True
        btnIndexarOrigen.Text = "⏹ Cancelar Indexado"
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
                                                              lblStatus.Text = $"{fase} {proc:#,##0}/{tot:#,##0}"
                                                              lblResumenIndice.Text = $"{fase} {proc:#,##0}/{tot:#,##0}"
                                                              Dim pct = CInt((proc / CDbl(tot)) * 100)
                                                              progressBar1.Value = Math.Min(100, Math.Max(0, pct))
                                                          Else
                                                              lblStatus.Text = fase
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
                MessageBox.Show($"Indexación completada exitosamente:{vbCrLf}{vbCrLf}" &
                                $"• Total PDFs indexados: {resumenFinal.TotalPdfs:#,##0}{vbCrLf}" &
                                $"• Total páginas registradas: {resumenFinal.TotalPaginas:#,##0}{vbCrLf}" &
                                $"• PDFs sin leer/corruptos: {resumenFinal.TotalSinLeer:#,##0}",
                                "Índice Actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As OperationCanceledException
            lblStatus.Text = "Indexación cancelada por el usuario."
            lblResumenIndice.Text = "Indexación cancelada."
            lblResumenIndice.ForeColor = Color.FromArgb(160, 40, 40)
        Catch ex As Exception
            lblStatus.Text = "Error en indexación: " & ex.Message
            lblResumenIndice.Text = "Error en indexación."
            lblResumenIndice.ForeColor = Color.FromArgb(180, 20, 20)
            MessageBox.Show("Error durante la indexación: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            estaIndexando = False
            btnIndexarOrigen.Text = "📑 Indexar Origen"
            If cancelSourceIndexar IsNot Nothing Then
                cancelSourceIndexar.Dispose()
                cancelSourceIndexar = Nothing
            End If
            ConfigurarUiEstado(False)
            ActualizarResumenIndiceEnUi(txtOrigen.Text.Trim())
        End Try
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
                ActualizarResumenIndiceEnUi(txtOrigen.Text.Trim())
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
        Dim swCronometroTotal = Diagnostics.Stopwatch.StartNew()

        Try
            Await Task.Run(Sub()
                               ' 1. Obtener todos los archivos PDF en origen
                               Me.BeginInvoke(Sub() lblStatus.Text = "Buscando archivos PDF en la carpeta de origen...")
                               Dim archivosPdf = Directory.EnumerateFiles(origen, "*.pdf", SearchOption.AllDirectories).ToArray()
                               Dim total = archivosPdf.Length

                               If total = 0 Then
                                   Me.BeginInvoke(Sub()
                                                      lblStatus.Text = "No se encontraron archivos PDF en la carpeta de origen."
                                                      ConfigurarUiEstado(False)
                                                  End Sub)
                                   Exit Sub
                               End If

                               ' 2. Cargar la base de datos completa en memoria en UN SOLO VIAJE (O(1))
                               Me.BeginInvoke(Sub() lblStatus.Text = "Cargando índice de base de datos en memoria para máxima velocidad...")
                               Dim mapaBd = ModuloConversionJpg.CargarDiccionarioBitacoraEnMemoria()
                               Dim mapaIndice = ModuloIndiceOrigen.CargarIndiceDirectorio(origen)

                               ' 3. Análisis masivo multinúcleo concurrente
                               Dim numHilos = ModuloRecursos.ObtenerHilosRedOptimos()
                               EscribirLogReparador($"[INICIO_ANALISIS] Origen='{origen}' | Destino='{destinoEfectivo}' | TotalPDFs={total:#,##0} | Hilos={numHilos} | RegistrosEnBD={If(mapaBd IsNot Nothing, mapaBd.Count, 0):#,##0}")

                               Dim contadorProgreso As Integer = 0
                               Dim swRelojUI = Diagnostics.Stopwatch.StartNew()
                               Dim bagItems As New ConcurrentBag(Of ItemDiagnostico)()

                               Dim popt As New ParallelOptions With {
                                   .MaxDegreeOfParallelism = numHilos,
                                   .CancellationToken = token
                               }

                               Parallel.ForEach(archivosPdf, popt, Sub(rutaPdf, loopState)
                                   If token.IsCancellationRequested Then
                                       loopState.Stop()
                                       Exit Sub
                                   End If

                                   Try
                                       Dim item = AnalizarUnPdfOptimizado(origen, destinoEfectivo, rutaPdf, mapaBd, mapaIndice)
                                       bagItems.Add(item)

                                       If item.Estado.StartsWith("⚠️") OrElse item.Estado.StartsWith("📁") Then
                                           Interlocked.Increment(totalIncompletos)
                                       ElseIf item.Estado.StartsWith("❌") Then
                                           Interlocked.Increment(totalFaltantes)
                                       Else
                                           Interlocked.Increment(totalCorrectos)
                                       End If
                                   Catch exItem As Exception
                                       Dim errItem As New ItemDiagnostico With {
                                           .RutaPdf = rutaPdf,
                                           .Documento = Path.GetFileName(rutaPdf),
                                           .CarpetaOrigen = Path.GetDirectoryName(rutaPdf),
                                           .Estado = "❌ Error lectura/red",
                                           .Detalle = exItem.Message,
                                           .EsIncompletoOFaltante = True,
                                           .Seleccionado = True
                                       }
                                       bagItems.Add(errItem)
                                       Interlocked.Increment(totalFaltantes)
                                       EscribirLogReparador($"[ERROR_CRITICO_ANALISIS_HILO] {rutaPdf} -> {exItem.GetType().Name}: {exItem.Message}")
                                   Finally
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
                                   End Try
                               End Sub)

                               ' 4. Verificación Bidireccional: Detectar Registros Huérfanos o Duplicados en BD
                               Me.BeginInvoke(Sub() lblStatus.Text = "Verificando consistencia con la base de datos (duplicados y huérfanos)...")
                               Try
                                   Dim nombreCarpetaOrigen = Path.GetFileName(origen.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                                   Dim registrosBd = ModuloConversionJpg.ObtenerRegistrosBitacoraPorCarpeta(nombreCarpetaOrigen)

                                   If registrosBd IsNot Nothing AndAlso registrosBd.Count > 0 Then
                                       ' A. Detección de Duplicados en BD
                                       Dim grupos = registrosBd.GroupBy(Function(r) If(Not String.IsNullOrEmpty(r.Documento), r.Documento, r.NombreOriginal), StringComparer.OrdinalIgnoreCase).
                                                                Where(Function(g) Not String.IsNullOrEmpty(g.Key))

                                       For Each g In grupos
                                           If g.Count() > 1 Then
                                               Dim ordenados = g.OrderByDescending(Function(r) If(r.Estatus = "OK", 1, 0)).ThenByDescending(Function(r) r.Id).ToList()
                                               Dim idValido = ordenados.First().Id
                                               For Each dup In ordenados.Skip(1)
                                                   Dim itemDup As New ItemDiagnostico With {
                                                       .IdBd = dup.Id,
                                                       .Documento = dup.Documento,
                                                       .CarpetaOrigen = dup.CarpetaOrigen,
                                                       .CarpetaDestino = dup.CarpetaDestino,
                                                       .PagsEsperadas = dup.Paginas,
                                                       .JpgsFisicos = 0,
                                                       .EnBd = $"Duplicado (ID {dup.Id})",
                                                       .Estado = "👥 Duplicado en BD",
                                                       .Detalle = $"Registro duplicado en BD (ID: {dup.Id}). Se conservará ID {idValido}. Al reparar se depurará de la BD para cuadrar.",
                                                       .EsIncompletoOFaltante = True,
                                                       .Seleccionado = True
                                                   }
                                                   bagItems.Add(itemDup)
                                                   Interlocked.Increment(totalIncompletos)
                                                   EscribirLogReparador($"[BD_DUPLICADO] Doc='{dup.Documento}' | ID Duplicado={dup.Id} | ID Válido={idValido}")
                                               Next
                                           End If
                                       Next

                                       ' B. Detección de Huérfanos en BD (Existen en BD pero NO existen físicamente en disco)
                                       Dim setNombresFisicos As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                                       For Each p In archivosPdf
                                           setNombresFisicos.Add(Path.GetFileName(p))
                                           setNombresFisicos.Add(Path.GetFileNameWithoutExtension(p))
                                       Next

                                       For Each reg In registrosBd
                                           Dim nomSinExt = Path.GetFileNameWithoutExtension(reg.Documento)
                                           Dim nomConExt = Path.GetFileName(reg.Documento)
                                           Dim origSinExt = Path.GetFileNameWithoutExtension(reg.NombreOriginal)
                                           Dim origConExt = Path.GetFileName(reg.NombreOriginal)

                                           Dim existeFisico = setNombresFisicos.Contains(nomSinExt) OrElse
                                                              setNombresFisicos.Contains(nomConExt) OrElse
                                                              setNombresFisicos.Contains(origSinExt) OrElse
                                                              setNombresFisicos.Contains(origConExt)

                                           If Not existeFisico Then
                                               Dim idReg = reg.Id
                                               Dim yaAgregado = bagItems.Any(Function(it) it.IdBd = idReg)
                                               If Not yaAgregado Then
                                                   Dim itemHuerfano As New ItemDiagnostico With {
                                                       .IdBd = reg.Id,
                                                       .Documento = reg.Documento,
                                                       .CarpetaOrigen = reg.CarpetaOrigen,
                                                       .CarpetaDestino = reg.CarpetaDestino,
                                                       .PagsEsperadas = reg.Paginas,
                                                       .JpgsFisicos = 0,
                                                       .EnBd = $"Huérfano (ID {reg.Id})",
                                                       .Estado = "👻 Huérfano en BD",
                                                       .Detalle = $"Existe en BD (ID: {reg.Id}) pero el archivo PDF físico ya no existe en la carpeta de origen. Al reparar se depurará de la BD para cuadrar.",
                                                       .EsIncompletoOFaltante = True,
                                                       .Seleccionado = True
                                                   }
                                                   bagItems.Add(itemHuerfano)
                                                   Interlocked.Increment(totalIncompletos)
                                                   EscribirLogReparador($"[BD_HUERFANO] Doc='{reg.Documento}' | ID={reg.Id} | Origen='{reg.CarpetaOrigen}'")
                                               End If
                                           End If
                                       Next
                                   End If
                               Catch exBdCheck As Exception
                                   EscribirLogReparador($"[ERROR_CHECK_BD_BIDIRECCIONAL] {exBdCheck.Message}")
                               End Try

                               listaCompletaItems = bagItems.OrderBy(Function(i) i.Documento).ToList()
                           End Sub, token)

            ActualizarVistaGrilla()
            lblStatus.Text = $"Análisis finalizado a máxima velocidad. {totalIncompletos:#,##0} incompletos y {totalFaltantes:#,##0} faltantes detectados."
            lblEstadisticas.Text = $"Total: {listaCompletaItems.Count:#,##0} | Correctos: {totalCorrectos:#,##0} | Incompletos: {totalIncompletos:#,##0} | Faltantes: {totalFaltantes:#,##0}"

            btnReparar.Enabled = (totalIncompletos + totalFaltantes > 0)
            EscribirLogReparador($"[FIN_ANALISIS] Total={listaCompletaItems.Count:#,##0} | Correctos={totalCorrectos:#,##0} | Incompletos={totalIncompletos:#,##0} | Faltantes={totalFaltantes:#,##0} | Tiempo={swCronometroTotal.Elapsed.TotalSeconds:F2}s")

        Catch ex As OperationCanceledException
            lblStatus.Text = "Análisis detenido por el usuario."
            EscribirLogReparador("[CANCELADO_ANALISIS] Operación cancelada por el usuario.")
        Catch ex As Exception
            Dim mensajeError As String = ex.Message
            If TypeOf ex Is AggregateException Then
                Dim agg = DirectCast(ex, AggregateException).Flatten()
                mensajeError = String.Join(Environment.NewLine, agg.InnerExceptions.Select(Function(ie) $"{ie.GetType().Name}: {ie.Message}"))
            ElseIf ex.InnerException IsNot Nothing Then
                mensajeError &= Environment.NewLine & $"Detalle: {ex.InnerException.Message}"
            End If
            EscribirLogReparador($"[EXCEPCION_GENERAL_ANALISIS] {mensajeError}")
            MessageBox.Show("Error durante el análisis:" & Environment.NewLine & Environment.NewLine & mensajeError, "Error en Análisis", MessageBoxButtons.OK, MessageBoxIcon.Error)
            lblStatus.Text = "Error en el análisis."
        Finally
            ConfigurarUiEstado(False)
        End Try
    End Sub

    ''' <summary>
    ''' Auditoría ultrarrápida: consulta la BD y el índice de origen en RAM (0 ms) y solo examina a fondo los que tienen discrepancias físicas.
    ''' </summary>
    Private Function AnalizarUnPdfOptimizado(
        origenBase As String,
        destinoBase As String,
        rutaPdf As String,
        mapaBd As Dictionary(Of String, ModuloConversionJpg.InfoRegistroBd),
        Optional mapaIndice As Dictionary(Of String, ModuloIndiceOrigen.InfoIndicePdf) = Nothing
    ) As ItemDiagnostico
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

        ' 0. Obtener páginas esperadas y tamaño del índice permanente en memoria si existe (0 ms / O(1))
        Dim pagsIndice As Integer = 0
        If mapaIndice IsNot Nothing Then
            Dim infoIndice As ModuloIndiceOrigen.InfoIndicePdf = Nothing
            If mapaIndice.TryGetValue(rutaPdf, infoIndice) Then
                pagsIndice = infoIndice.Paginas
                item.TamanoBytes = infoIndice.Tamano
            ElseIf mapaIndice.TryGetValue(item.Documento, infoIndice) Then
                pagsIndice = infoIndice.Paginas
                item.TamanoBytes = infoIndice.Tamano
            End If
        End If

        If item.TamanoBytes <= 0 Then
            Try
                Dim fi As New FileInfo(rutaPdf)
                If fi.Exists Then item.TamanoBytes = fi.Length
            Catch
            End Try
        End If

        Try
            ' 1. Consulta ultrarrápida en memoria (O(1))
            Dim infoBd As ModuloConversionJpg.InfoRegistroBd = Nothing
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
                item.JpgsFisicos = 0
                ' Priorizar índice en memoria (0 ms), luego BD, y como último recurso leer por red
                Dim pags As Integer = pagsIndice
                If pags <= 0 AndAlso infoBd IsNot Nothing AndAlso infoBd.Paginas > 0 Then
                    pags = infoBd.Paginas
                End If
                If pags <= 0 Then
                    pags = ModuloConversionJpg.ObtenerTotalPaginasPdf(rutaPdf)
                End If
                item.PagsEsperadas = pags
                item.Detalle = If(pags > 0,
                    $"La carpeta destino no existe físicamente en el almacenamiento. (Se creará con sus {pags} páginas al reparar)",
                    "La carpeta destino no existe y no se pudieron leer las páginas del PDF origen.")
                item.EsIncompletoOFaltante = True
                item.Seleccionado = True
                EscribirLogReparador($"[ANALISIS_FALTANTE_TOTAL] PDF='{rutaPdf}' | Pags={pags} | Destino='{carpetaDestinoEsperada}'")
                Return item
            End If

            Dim archivosJpg = Directory.GetFiles(carpetaDestinoEsperada, "*.jpg")
            item.JpgsFisicos = archivosJpg.Length

            If archivosJpg.Length = 0 Then
                item.Estado = "📁 Carpeta vacía"
                ' Priorizar índice en memoria (0 ms), luego BD, y como último recurso leer por red
                Dim pags As Integer = pagsIndice
                If pags <= 0 AndAlso infoBd IsNot Nothing AndAlso infoBd.Paginas > 0 Then
                    pags = infoBd.Paginas
                End If
                If pags <= 0 Then
                    pags = ModuloConversionJpg.ObtenerTotalPaginasPdf(rutaPdf)
                End If
                item.PagsEsperadas = pags
                item.Detalle = If(pags > 0,
                    $"La carpeta existe pero está vacía (0 imágenes JPG generadas). Se reparará con sus {pags} páginas.",
                    "La carpeta existe pero está vacía y no se pudieron leer las páginas del PDF origen.")
                item.EsIncompletoOFaltante = True
                item.Seleccionado = True
                EscribirLogReparador($"[ANALISIS_CARPETA_VACIA] PDF='{rutaPdf}' | Pags={pags} | Destino='{carpetaDestinoEsperada}'")
                Return item
            End If

            ' 3. Vía rápida para los ya completados y registrados en BD (Evita leer 100,000 PDFs por red SMB)
            If infoBd IsNot Nothing AndAlso infoBd.Estatus = "OK" AndAlso infoBd.Paginas > 0 AndAlso infoBd.Paginas = archivosJpg.Length Then
                item.PagsEsperadas = infoBd.Paginas
                item.Estado = "✔️ Correcto (OK)"
                item.Detalle = "Físico completo y verificado contra registro en BD."
                item.EsIncompletoOFaltante = False
                item.Seleccionado = False
                Return item
            End If

            ' 3b. Vía rápida usando el índice permanente en memoria si coincide exactamente con los JPGs físicos
            If pagsIndice > 0 AndAlso pagsIndice = archivosJpg.Length Then
                item.PagsEsperadas = pagsIndice
                Dim errInt As String = ""
                If ModuloConversionJpg.ValidarIntegridadDiagnosticoRapidoJpg(carpetaDestinoEsperada, nombreSinExt, pagsIndice, errInt) Then
                    If infoBd IsNot Nothing AndAlso infoBd.Estatus = "OK" Then
                        item.Estado = "✔️ Correcto (OK)"
                        item.Detalle = "Físico completo y verificado contra índice y BD."
                        item.EsIncompletoOFaltante = False
                        item.Seleccionado = False
                    Else
                        item.Estado = "⚠️ Físico OK (Falta en BD)"
                        item.Detalle = "Imágenes JPG completas en disco según índice pero falta confirmar en BD."
                        item.EsIncompletoOFaltante = True
                        item.Seleccionado = True
                    End If
                    Return item
                End If
            End If

            ' 4. Si hay discrepancia o no está en BD, examinar las páginas esperadas (Índice en RAM primero, SMB como fallback)
            Dim pagsEsperadas As Integer = pagsIndice
            If pagsEsperadas <= 0 AndAlso infoBd IsNot Nothing AndAlso infoBd.Paginas > 0 Then
                pagsEsperadas = infoBd.Paginas
            End If
            If pagsEsperadas <= 0 Then
                pagsEsperadas = ModuloConversionJpg.ObtenerTotalPaginasPdf(rutaPdf)
            End If
            item.PagsEsperadas = pagsEsperadas

            If pagsEsperadas <= 0 Then
                item.Estado = "❌ PDF no legible o corrupto"
                item.Detalle = "No se pudieron leer las páginas del PDF origen (0 páginas)"
                item.EsIncompletoOFaltante = True
                item.Seleccionado = True
                EscribirLogReparador($"[ERROR_PAGINAS_CERO] PDF='{rutaPdf}' no devolvió páginas legibles en ningún motor.")
                Return item
            End If

            If archivosJpg.Length < pagsEsperadas Then
                Dim faltan = pagsEsperadas - archivosJpg.Length
                item.Estado = $"⚠️ Incompleto (faltan {faltan} págs)"
                item.Detalle = $"Se esperaban {pagsEsperadas} JPGs pero solo hay {archivosJpg.Length}."
                item.EsIncompletoOFaltante = True
                item.Seleccionado = True
                EscribirLogReparador($"[ANALISIS_INCOMPLETO] PDF='{rutaPdf}' | Esperadas={pagsEsperadas} | Encontradas={archivosJpg.Length} | Faltan={faltan}")
                Return item
            End If

            ' 5. Validación de integridad física estricta
            Dim errorIntegridad As String = ""
            Dim validoFisicamente = ModuloConversionJpg.ValidarIntegridadDiagnosticoRapidoJpg(carpetaDestinoEsperada, nombreSinExt, pagsEsperadas, errorIntegridad)

            If Not validoFisicamente Then
                item.Estado = "⚠️ Incompleto (Daño)"
                item.Detalle = errorIntegridad
                item.EsIncompletoOFaltante = True
                item.Seleccionado = True
                EscribirLogReparador($"[ANALISIS_INTEGRIDAD_FALLIDA] PDF='{rutaPdf}' | Detalle='{errorIntegridad}'")
                Return item
            End If

            ' 6. Verificación de concordancia final
            If infoBd IsNot Nothing AndAlso infoBd.Estatus = "OK" Then
                item.Estado = "✔️ Correcto (OK)"
                item.Detalle = "Físico completo y registrado en BD con estatus OK."
                item.EsIncompletoOFaltante = False
                item.Seleccionado = False
            Else
                item.Estado = "⚠️ Físico OK (Falta en BD)"
                item.Detalle = "Imágenes JPG completas en disco pero falta confirmar en BD."
                item.EsIncompletoOFaltante = True
                item.Seleccionado = True
                EscribirLogReparador($"[ANALISIS_FALTA_EN_BD] PDF='{rutaPdf}' | Físico tiene {archivosJpg.Length} JPGs pero falta en BD.")
            End If

            Return item

        Catch ex As Exception
            item.Estado = "❌ Error lectura/red"
            item.Detalle = $"Error al inspeccionar archivo o red: {ex.Message}"
            item.EsIncompletoOFaltante = True
            item.Seleccionado = True
            EscribirLogReparador($"[ERROR_ANALISIS_ARCHIVO] {rutaPdf} -> {ex.GetType().Name}: {ex.Message}")
            Return item
        End Try
    End Function

    Private Function AnalizarUnPdf(origenBase As String, destinoBase As String, rutaPdf As String) As ItemDiagnostico
        Return AnalizarUnPdfOptimizado(origenBase, destinoBase, rutaPdf, Nothing, Nothing)
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
                item.JpgsFisicos,
                item.EnBd,
                item.CarpetaDestino,
                item.CarpetaOrigen,
                item.Detalle
            )
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
            ElseIf item.Estado.StartsWith("👻") OrElse item.Estado.StartsWith("👥") Then
                fila.Cells(ColEstado.Index).Style.BackColor = Color.FromArgb(245, 230, 255)
                fila.Cells(ColEstado.Index).Style.ForeColor = Color.FromArgb(120, 30, 160)
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

        ' Recoger elementos marcados para reparar y ordenar estrictamente de mayor a menor peso (más pesados primero)
        SincronizarSeleccionDesdeGrilla()
        Dim aReparar = listaCompletaItems.Where(Function(it) it.Seleccionado AndAlso it.EsIncompletoOFaltante) _
            .OrderByDescending(Function(it)
                                   If it.TamanoBytes > 0 Then Return it.TamanoBytes
                                   Try
                                       Dim fi As New FileInfo(it.RutaPdf)
                                       If fi.Exists Then
                                           it.TamanoBytes = fi.Length
                                           Return it.TamanoBytes
                                       End If
                                   Catch
                                   End Try
                                   Return 0L
                               End Function) _
            .ToList()

        If aReparar.Count = 0 Then
            MessageBox.Show("No hay archivos incompletos o faltantes seleccionados para reparar.", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If

        Dim total = aReparar.Count
        Dim pesoMaxMB = If(total > 0, (aReparar.First().TamanoBytes / (1024.0 * 1024.0)), 0.0)
        Dim pesoMinMB = If(total > 0, (aReparar.Last().TamanoBytes / (1024.0 * 1024.0)), 0.0)

        Dim resp = MessageBox.Show(
            $"Se repararán {aReparar.Count} archivos detectados con discrepancias o incompletos.{vbCrLf}" &
            $"Prioridad: Ordenados por peso (de mayor a menor peso: {pesoMaxMB:0.1} MB máx -> {pesoMinMB:0.1} MB mín).{vbCrLf}{vbCrLf}" &
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

        ConfigurarUiEstado(True, $"Iniciando reparación de {total} archivos (Más pesados primero: {pesoMaxMB:0.1} MB -> {pesoMinMB:0.1} MB)...")
        cancelSource = New CancellationTokenSource()
        Dim token = cancelSource.Token

        Dim procesados As Integer = 0
        Dim reparadosOk As Integer = 0
        Dim fallados As Integer = 0

        progressBar1.Value = 0

        Dim maxHilos = ModuloRecursos.ObtenerHilosOptimos()
        Dim limiteGrandeBytes As Long = ModuloRecursos.LimitePdfGrandeBytes
        Dim colaGrandes As New ConcurrentQueue(Of ItemDiagnostico)(aReparar.Where(Function(i) i.TamanoBytes > limiteGrandeBytes))
        Dim colaPequenos As New ConcurrentQueue(Of ItemDiagnostico)(aReparar.Where(Function(i) i.TamanoBytes <= limiteGrandeBytes))

        Dim totalGrandes = colaGrandes.Count
        Dim totalPequenos = colaPequenos.Count
        Dim maxGrandesEnParalelo = Math.Max(1, Math.Min(20, maxHilos))

        EscribirLogReparador($"[INICIO_REPARACION] Total a reparar={total} (Grandes >30MB: {totalGrandes} [tope simultáneo {maxGrandesEnParalelo}] | Ligeros <=30MB: {totalPequenos} [hasta {maxHilos} hilos / 1 por núcleo]). CERO ESPERAS.")

        Try
            Await Task.Run(Sub()
                               Dim procesarItem = Sub(item As ItemDiagnostico)
                                   If token.IsCancellationRequested Then Exit Sub

                                   ' Manejo de registros huérfanos o duplicados en BD (depurar de BD para cuadrar cifras)
                                   If item.IdBd > 0 AndAlso (item.Estado.Contains("Duplicado") OrElse item.Estado.Contains("Huérfano")) Then
                                       Dim borrado = ModuloConversionJpg.EliminarRegistroDeBitacoraPorId(item.IdBd)
                                       If borrado Then
                                           item.Estado = "✔️ Depurado de BD"
                                           item.Detalle = $"Registro ID {item.IdBd} eliminado de la base de datos con éxito. BD cuadrada."
                                           item.EsIncompletoOFaltante = False
                                           item.Seleccionado = False
                                           Interlocked.Increment(reparadosOk)
                                           EscribirLogReparador($"[DEPURADO_BD_OK] ID={item.IdBd} | Doc='{item.Documento}'")
                                       Else
                                           item.Estado = "❌ Error al depurar BD"
                                           item.Detalle = $"No se pudo eliminar el registro ID {item.IdBd} de la base de datos."
                                           Interlocked.Increment(fallados)
                                           EscribirLogReparador($"[ERROR_DEPURADO_BD] ID={item.IdBd} | Doc='{item.Documento}'")
                                       End If

                                       Dim actualDep = Interlocked.Increment(procesados)
                                       Dim pctDep = CInt((actualDep / CDbl(total)) * 100)
                                       Me.BeginInvoke(Sub()
                                                          progressBar1.Value = Math.Min(100, pctDep)
                                                          lblStatus.Text = $"Depurando BD ({actualDep}/{total}): {item.Documento} -> {item.Estado}"
                                                          lblEstadisticas.Text = $"Total a reparar: {total} | Reparados/Depurados OK: {reparadosOk} | Errores: {fallados}"
                                                      End Sub)
                                       Exit Sub
                                   End If

                                   Dim nombreSinExt = Path.GetFileNameWithoutExtension(item.Documento)
                                   Dim carpetaDestino = item.CarpetaDestino

                                   ' 1. Vaciar archivos incompletos previos si la carpeta ya existía
                                   ModuloConversionJpg.VaciarArchivosDeCarpeta(carpetaDestino)

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
                                       If ModuloConversionJpg.ValidarIntegridadDiagnosticoRapidoJpg(carpetaDestino, nombreSinExt, totalPags, msgError) Then
                                           exito = True
                                       Else
                                           exito = False
                                           ModuloConversionJpg.VaciarArchivosDeCarpeta(carpetaDestino)
                                       End If

                                   Catch exOp As OperationCanceledException
                                       ModuloConversionJpg.VaciarArchivosDeCarpeta(carpetaDestino)
                                       Exit Sub
                                   Catch exGen As Exception
                                       exito = False
                                       Dim errDetalle As String = exGen.Message
                                       If TypeOf exGen Is AggregateException Then
                                           Dim agg = DirectCast(exGen, AggregateException).Flatten()
                                           errDetalle = String.Join(" | ", agg.InnerExceptions.Select(Function(ie) ie.Message))
                                       ElseIf exGen.InnerException IsNot Nothing Then
                                           errDetalle &= " | " & exGen.InnerException.Message
                                       End If
                                       msgError = errDetalle
                                       ModuloConversionJpg.VaciarArchivosDeCarpeta(carpetaDestino)
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
                                       EscribirLogReparador($"[REPARADO_OK] PDF='{item.RutaPdf}' -> {totalPags} páginas generadas en '{carpetaDestino}'")
                                   Else
                                       item.Estado = "❌ Error al reparar"
                                       item.Detalle = msgError
                                       Interlocked.Increment(fallados)
                                       EscribirLogReparador($"[ERROR_REPARACION] PDF='{item.RutaPdf}' -> Error: {msgError}")
                                   End If

                                   Dim actual = Interlocked.Increment(procesados)
                                   Dim pct = CInt((actual / CDbl(total)) * 100)

                                   Me.BeginInvoke(Sub()
                                                      progressBar1.Value = Math.Min(100, pct)
                                                      Dim mbDoc = item.TamanoBytes / (1024.0 * 1024.0)
                                                      lblStatus.Text = $"Reparando ({actual}/{total}) [{mbDoc:0.1} MB]: {item.Documento} -> {item.Estado}"
                                                      lblEstadisticas.Text = $"Total a reparar: {total} | Reparados OK: {reparadosOk} | Errores: {fallados}"
                                                  End Sub)
                               End Sub

                               Dim grandesEnVuelo As Integer = 0
                               Dim lockerCola As New Object()
                               Dim tareasConsumidoras As New List(Of Task)()

                               For h As Integer = 1 To maxHilos
                                   tareasConsumidoras.Add(Task.Run(Sub()
                                       While Not token.IsCancellationRequested
                                           Dim itemAProcesar As ItemDiagnostico = Nothing
                                           Dim esGrande As Boolean = False

                                           SyncLock lockerCola
                                               While Not token.IsCancellationRequested
                                                   If colaGrandes.IsEmpty AndAlso colaPequenos.IsEmpty Then
                                                       Exit While
                                                   End If

                                                   If grandesEnVuelo < maxGrandesEnParalelo AndAlso colaGrandes.TryDequeue(itemAProcesar) Then
                                                       grandesEnVuelo += 1
                                                       esGrande = True
                                                       Exit While
                                                   End If

                                                   If colaPequenos.TryDequeue(itemAProcesar) Then
                                                       esGrande = False
                                                       Exit While
                                                   End If

                                                   Monitor.Wait(lockerCola, 50)
                                               End While
                                           End SyncLock

                                           If itemAProcesar Is Nothing Then Exit While

                                           Try
                                               procesarItem(itemAProcesar)
                                           Finally
                                               If esGrande Then
                                                   SyncLock lockerCola
                                                       grandesEnVuelo -= 1
                                                       Monitor.PulseAll(lockerCola)
                                                   End SyncLock
                                               End If
                                           End Try
                                       End While
                                   End Sub, token))
                               Next

                               Task.WaitAll(tareasConsumidoras.ToArray())
                           End Sub, token)

            ActualizarVistaGrilla()
            EscribirLogReparador($"[FIN_REPARACION] Total={total} | ReparadosOK={reparadosOk} | Fallados={fallados}")
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
            Dim mensajeError As String = ex.Message
            If TypeOf ex Is AggregateException Then
                Dim agg = DirectCast(ex, AggregateException).Flatten()
                mensajeError = String.Join(Environment.NewLine, agg.InnerExceptions.Select(Function(ie) $"{ie.GetType().Name}: {ie.Message}"))
            ElseIf ex.InnerException IsNot Nothing Then
                mensajeError &= Environment.NewLine & $"Detalle: {ex.InnerException.Message}"
            End If
            EscribirLogReparador($"[EXCEPCION_GENERAL_REPARACION] {mensajeError}")
            MessageBox.Show("Error durante la reparación:" & Environment.NewLine & Environment.NewLine & mensajeError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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

    Private Shared ReadOnly _lockLogReparador As New Object()

    Private Sub EscribirLogReparador(mensaje As String)
        Try
            Dim linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {mensaje}"
            Dim nombresArchivos = {"depuracion_reparador.txt", "depuracion_reparador.log"}

            Dim carpetasDestino As New List(Of String)()
            Dim agregarCarpeta = Sub(ruta As String)
                If String.IsNullOrWhiteSpace(ruta) Then Exit Sub
                Try
                    If Directory.Exists(ruta) Then
                        Dim normalizada = Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        If Not carpetasDestino.Any(Function(c) c.Equals(normalizada, StringComparison.OrdinalIgnoreCase)) Then
                            carpetasDestino.Add(normalizada)
                        End If
                    End If
                Catch
                End Try
            End Sub

            agregarCarpeta(Application.StartupPath)
            agregarCarpeta(AppDomain.CurrentDomain.BaseDirectory)
            agregarCarpeta(Path.GetTempPath())

            SyncLock _lockLogReparador
                For Each carpeta In carpetasDestino
                    For Each nombre In nombresArchivos
                        Try
                            Dim rutaArchivo = Path.Combine(carpeta, nombre)
                            File.AppendAllText(rutaArchivo, linea & Environment.NewLine, Encoding.UTF8)
                        Catch
                        End Try
                    Next
                Next
            End SyncLock
        Catch
        End Try
    End Sub

End Class
