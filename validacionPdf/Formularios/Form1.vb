Imports System.IO
Imports System.Text.RegularExpressions
Imports iText.Kernel.Pdf

Public Class Form1

    Dim rutaArchivo As String = ""
    Dim listaPDFs As List(Of String)
    Dim indiceActual As Integer = 0
    Dim totalRenombrados As Integer = 0
    Dim totalDuplicados As Integer = 0
    Dim totalEliminados As Integer = 0

    ' Doble captura
    Dim capturaPendiente As String = ""
    Dim esperandoConfirmacion As Boolean = False
    Dim pdfFueRenombrado As Boolean = False
    Dim pdfFueRotado As Boolean = False
    Private anguloCarga As Integer = 0

#Region "Initialization"
    '=============================
    ' FORM LOAD
    '=============================
    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Await WebView21.EnsureCoreWebView2Async()
            ActualizarContadores()

            ' Mostrar usuario, directorio y lote en el título
            Dim titulo = "Validación PDF"
            If ModuloUsuarios.UsuarioActual IsNot Nothing Then
                titulo &= " — " & ModuloUsuarios.UsuarioActual.NombreCompleto
            End If
            If ModuloDirectorios.DirectorioActual IsNot Nothing Then
                titulo &= " | " & ModuloDirectorios.DirectorioActual.Nombre
            End If
            If Not String.IsNullOrEmpty(ModuloDirectorios.LoteActual) Then
                titulo &= " › " & ModuloDirectorios.LoteActual
            End If
            Me.Text = titulo

            ModuloCatalogo.InicializarCatalogo()

            ' Cargar PDFs del lote seleccionado automáticamente
            If Not String.IsNullOrEmpty(ModuloDirectorios.RutaLoteActual) Then
                CargarPDFsDesdeRuta(ModuloDirectorios.RutaLoteActual)
            End If
        Catch ex As Exception
            MessageBox.Show("Error en Form1_Load: " & ex.Message & vbCrLf & ex.StackTrace)
        End Try
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            ModuloDirectorios.LiberarLoteActual()
        Catch ex As Exception
            ' Continuar cierre sin bloquear la UI
        End Try
    End Sub

    Private Sub ActualizarContadores()
        lbl.Text = "Renombrados: " & totalRenombrados &
                   " | Duplicados: " & totalDuplicados &
                   " | Eliminados: " & totalEliminados
    End Sub



    Private Async Sub CargarPDFsDesdeRuta(ruta As String)
        Try
            If Not Directory.Exists(ruta) Then
                MessageBox.Show("La ruta del lote no existe: " & ruta)
                Exit Sub
            End If

            Dim frmCarga As New FormCargando()
            frmCarga.Show(Me)
            Application.DoEvents()

            btnSiguiente.Enabled = False
            btnSeleccionarCarpeta.Enabled = False

            ' Buscar en hilo secundario para no congelar la UI
            listaPDFs = Await Task.Run(Function() Directory.GetFiles(ruta, "*.pdf").ToList())

            frmCarga.Close()
            frmCarga.Dispose()

            btnSiguiente.Enabled = True
            btnSeleccionarCarpeta.Enabled = True

            If listaPDFs.Count = 0 Then
                lblNombreActual.Text = "No hay PDFs en este lote"
                lblTotalPdf.Text = ""
                lblEstado.Text = "Lote vacío"
                Exit Sub
            End If

            indiceActual = 0
            rutaArchivo = listaPDFs(0)
            MostrarPDF(rutaArchivo)
            ReiniciarCaptura()
            lblNombreActual.Text = "Nombre actual: " & Path.GetFileName(rutaArchivo)
            lblTotalPdf.Text = ModuloDirectorios.LoteActual & " | Mostrando PDF 1 de " & listaPDFs.Count
        Catch ex As Exception
            MessageBox.Show("Error en CargarPDFsDesdeRuta: " & ex.Message & vbCrLf & ex.StackTrace)
        End Try
    End Sub

    '=============================
    ' MOSTRAR PDF
    '=============================
    Private Async Sub MostrarPDF(rutaPdf As String)
        Await WebView21.EnsureCoreWebView2Async()

        ' Agregar timestamp para evitar cache de WebView2 (evita pantalla negra al rotar)
        Dim ruta As String = "file:///" & rutaPdf.Replace("\", "/") & "?t=" & DateTime.Now.Ticks.ToString()
        WebView21.CoreWebView2.Navigate(ruta)

        ' Actualizar el máximo de páginas en el NumericUpDown de rotación
        Try
            Using reader As New PdfReader(rutaPdf)
                Using doc As New PdfDocument(reader)
                    nudPagina.Maximum = doc.GetNumberOfPages()
                    nudPagina.Value = 1
                End Using
            End Using
        Catch
            nudPagina.Maximum = 1
            nudPagina.Value = 1
        End Try
    End Sub

    '=============================
    ' SELECCIONAR CARPETA
    '=============================
    ' Botón reutilizado como "Cambiar lote" — abre FormSeleccionSesion
    Private Sub btnSeleccionarCarpeta_Click(sender As Object, e As EventArgs) Handles btnSeleccionarCarpeta.Click

        ' Liberar lote actual
        ModuloDirectorios.LiberarLoteActual()

        Dim frmSesion As New FormSeleccionSesion()
        If frmSesion.ShowDialog() = DialogResult.OK Then
            totalRenombrados = 0
            totalDuplicados = 0
            totalEliminados = 0
            ActualizarContadores()
            rutaArchivo = ""
            listaPDFs = Nothing
            indiceActual = 0
            ReiniciarCaptura()
            WebView21.CoreWebView2.Navigate("about:blank")

            ' Actualizar título
            Dim titulo = "Validación PDF — " & ModuloUsuarios.UsuarioActual.NombreCompleto
            titulo &= " | " & ModuloDirectorios.DirectorioActual.Nombre
            titulo &= " › " & ModuloDirectorios.LoteActual
            Me.Text = titulo

            CargarPDFsDesdeRuta(ModuloDirectorios.RutaLoteActual)
        End If

    End Sub

    '=============================
    ' SIGUIENTE
    '=============================
    Private Sub btnSiguiente_Click(sender As Object, e As EventArgs) Handles btnSiguiente.Click
        If listaPDFs Is Nothing OrElse listaPDFs.Count = 0 Then Exit Sub

        ' Preguntar si está validado para mandarlo a entregables
        Dim resp = MessageBox.Show(
            "¿El documento ha sido validado correctamente?",
            "Confirmación",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question)

        If resp = DialogResult.Yes Then
            ' Mover a carpeta Entregable/{id_delegacion}
            Try
                Dim nombreArchivo = Path.GetFileName(rutaArchivo)
                Dim rutaDestino = ModuloDirectorios.RutaEntregable

                If String.IsNullOrEmpty(rutaDestino) Then
                    MessageBox.Show("No hay ruta de entregable configurada.")
                    Exit Sub
                End If

                ' Extraer año del nombre (formato esperado: idDelegacion_año_resto.pdf)
                Dim partes = nombreArchivo.Split("_"c)
                If partes.Length >= 2 Then
                    Dim anioStr = partes(1)
                    Dim anio As Integer
                    ' Verificar si es un año válido en el catálogo
                    If Integer.TryParse(anioStr, anio) AndAlso ModuloCatalogo.CargarAnios().Contains(anio) Then
                        rutaDestino = Path.Combine(rutaDestino, anioStr)
                        If Not Directory.Exists(rutaDestino) Then
                            Directory.CreateDirectory(rutaDestino)
                        End If
                    End If
                End If

                Dim destino = Path.Combine(rutaDestino, nombreArchivo)

                ' Si ya existe en destino, agregar sufijo
                If File.Exists(destino) Then
                    Dim sinExt = Path.GetFileNameWithoutExtension(nombreArchivo)
                    Dim ext = Path.GetExtension(nombreArchivo)
                    destino = Path.Combine(rutaDestino, sinExt & "_" & DateTime.Now.ToString("HHmmss") & ext)
                End If

                WebView21.CoreWebView2.Navigate("about:blank")
                Application.DoEvents()

                File.Move(rutaArchivo, destino)

                ' Construir detalle para auditoría
                Dim detalle = "Movido a carpeta de revisados"
                If pdfFueRotado Then
                    detalle = "Movido a carpeta de revisados (incluye rotación de páginas)"
                End If

                ModuloAuditoria.RegistrarAccion("Validado", nombreArchivo, Path.GetFileName(destino), detalle)

                ' Quitar de la lista
                listaPDFs.RemoveAt(indiceActual)

                If indiceActual >= listaPDFs.Count Then
                    indiceActual = listaPDFs.Count - 1
                End If

                ReiniciarCaptura()

                If listaPDFs.Count > 0 Then
                    CargarPDFActual()
                Else
                    MessageBox.Show("Proceso terminado ✅")
                    rutaArchivo = ""
                    lblNombreActual.Text = "Sin archivos"
                    lblTotalPdf.Text = ""
                End If

                Exit Sub

            Catch ex As Exception
                MessageBox.Show("Error al mover archivo: " & ex.Message)
            End Try

        ElseIf resp = DialogResult.No Then
            ' Desea saltar sin mandarlo a entregables
            Dim respSaltar = MessageBox.Show("¿Deseas saltar este archivo y dejarlo en el lote actual?", "Saltar archivo", MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If respSaltar = DialogResult.Yes Then
                ' Avanzar al siguiente sin mover
                If indiceActual < listaPDFs.Count - 1 Then
                    indiceActual += 1
                    ReiniciarCaptura()
                    CargarPDFActual()
                Else
                    MessageBox.Show("Ya no hay más PDFs adelante.")
                End If
            End If
        End If
    End Sub

#End Region

#Region "PDF Navigation and Display"
    '=============================
    ' CARGAR PDF ACTUAL
    '=============================
    Private Sub CargarPDFActual()
        rutaArchivo = listaPDFs(indiceActual)
        MostrarPDF(rutaArchivo)

        lblNombreActual.Text = "Nombre actual: " & Path.GetFileName(rutaArchivo)
        lblTotalPdf.Text = ModuloDirectorios.LoteActual & " | Mostrando PDF " & (indiceActual + 1) & " de " & listaPDFs.Count
    End Sub

    '=============================
    ' DOBLE CAPTURA — REINICIAR
    '=============================
    Private Sub ReiniciarCaptura()
        capturaPendiente = ""
        esperandoConfirmacion = False
        pdfFueRenombrado = False
        pdfFueRotado = False
        txtNombre.Clear()
        txtNombre.BackColor = Color.White
        lblCaptura.Text = "Primera captura"
        lblCaptura.ForeColor = Color.Black
    End Sub

#End Region

#Region "Validation Logic"
    '=============================
    ' VALIDACIÓN DE TEXTO
    '=============================
    Private Sub txtNombre_TextChanged(sender As Object, e As EventArgs) Handles txtNombre.TextChanged

        Dim errorMsg As String = ""

        If txtNombre.Text = "" Then
            txtNombre.BackColor = Color.White
            lblEstado.Text = "Esperando captura..."
            Exit Sub
        End If

        If ValidarNombreGeneral(txtNombre.Text, errorMsg) Then
            txtNombre.BackColor = Color.LightGreen
            lblEstado.Text = "Formato correcto ✅"
        Else
            txtNombre.BackColor = Color.LightCoral
            lblEstado.Text = errorMsg
        End If

    End Sub

    ' Bloquear pegado con Ctrl+V
    Private Sub txtNombre_KeyDown(sender As Object, e As KeyEventArgs) Handles txtNombre.KeyDown
        If e.Control AndAlso e.KeyCode = Keys.V Then
            e.SuppressKeyPress = True
        End If
    End Sub

    '=============================
    ' VALIDACIÓN REGLA 1
    '=============================
    Function ValidarRegla1(nombre As String, ByRef mensajeError As String) As Boolean

        ' Formato: 00_0000_00 a 000000
        If Not Regex.IsMatch(nombre, "^\d{2}_\d{4}_\d{2,6}$") Then
            mensajeError = "Formato inválido. Ej: 01_2024_123456"
            Return False
        End If

        ' Validar prefijo de delegación
        If ModuloDirectorios.DelegacionActual IsNot Nothing Then
            Dim prefijo = ModuloDirectorios.DelegacionActual.Id & "_"
            If Not nombre.StartsWith(prefijo) Then
                mensajeError = "Debe iniciar con " & prefijo & " (delegación " & ModuloDirectorios.DelegacionActual.NombreCompleto & ")"
                Return False
            End If
        End If

        ' Validar año contra catálogo
        Dim partes = nombre.Split("_")
        Dim anio As Integer
        If Integer.TryParse(partes(1), anio) Then
            If Not ModuloCatalogo.AnioEsValido(anio) Then
                mensajeError = "Año " & anio & " no está en el catálogo"
                Return False
            End If
        End If

        Return True

    End Function

    '=============================
    ' VALIDACIÓN REGLA 2
    '=============================
    Function ValidarRegla2(nombre As String, ByRef mensajeError As String) As Boolean

        Dim partes = nombre.Split("_")

        ' Mínimo requerido
        If partes.Length < 9 Then
            mensajeError = "Faltan bloques"
            Return False
        End If

        Dim total = partes.Length

        ' POSICIÓN FINAL (último)
        If Not Regex.IsMatch(partes(total - 1), "^\d{2}$") Then
            mensajeError = "Error en posición final"
            Return False
        End If

        ' FOLIO
        If Not Regex.IsMatch(partes(total - 2), "^\d{4}$") Then
            mensajeError = "Error en folio"
            Return False
        End If

        ' AÑO
        If Not Regex.IsMatch(partes(total - 3), "^\d{4}$") Then
            mensajeError = "Error en año"
            Return False
        End If

        ' Validar año contra catálogo
        Dim anio As Integer
        If Integer.TryParse(partes(total - 3), anio) Then
            If Not ModuloCatalogo.AnioEsValido(anio) Then
                mensajeError = "Año " & anio & " no está en el catálogo"
                Return False
            End If
        End If

        ' Posiciones 1-3
        For i = 0 To 2
            If Not Regex.IsMatch(partes(i), "^\d{2}$") Then
                mensajeError = "Error en posición " & (i + 1)
                Return False
            End If
        Next

        ' POSICIÓN 5
        Dim idx As Integer = 3
        Dim encontroPos5 As Boolean = False

        While idx < total - 3
            If Regex.IsMatch(partes(idx), "^\d{2}$") Then
                encontroPos5 = True
                Exit While
            End If
            idx += 1
        End While

        If Not encontroPos5 Then
            mensajeError = "No se encontró posición 5"
            Return False
        End If

        If Not Regex.IsMatch(partes(idx), "^\d{2}$") Then
            mensajeError = "Error en posición 5"
            Return False
        End If

        idx += 1

        ' POSICIÓN 6
        If idx >= total - 3 Then
            mensajeError = "Falta posición 6"
            Return False
        End If

        Dim bloquesPos6 = (total - 3) - idx

        If bloquesPos6 <= 0 Then
            mensajeError = "Error en posición 6"
            Return False
        End If

        If bloquesPos6 > 10 Then
            mensajeError = "Exceso de bloques en posición 6"
            Return False
        End If

        Return True

    End Function

    '=============================
    ' VALIDACIÓN GENERAL
    '=============================
    Function ValidarNombreGeneral(nombre As String, ByRef mensajeError As String) As Boolean

        If nombre.Contains("__") Then
            mensajeError = "Error: doble guion bajo"
            Return False
        End If

        If rbRegla1.Checked Then
            Return ValidarRegla1(nombre, mensajeError)
        End If

        If rbRegla2.Checked Then
            Return ValidarRegla2(nombre, mensajeError)
        End If

        mensajeError = "Selecciona validación"
        Return False

    End Function

    Private Sub rbRegla1_CheckedChanged(sender As Object, e As EventArgs) Handles rbRegla1.CheckedChanged
        If rbRegla1.Checked Then
            lblEstado.Text = "Ejemplo: 01_2024_1234 (último bloque de 2 a 6 dígitos)"
            txtNombre.Clear()
            txtNombre.BackColor = Color.White
        End If
    End Sub

    Private Sub rbRegla2_CheckedChanged(sender As Object, e As EventArgs) Handles rbRegla2.CheckedChanged
        If rbRegla2.Checked Then
            lblEstado.Text = "Ejemplo: 01_01_01_01_00_00_2011_0090_00"
            txtNombre.Clear()
            txtNombre.BackColor = Color.White
        End If
    End Sub

    '=============================
    ' DETECCIÓN DE SIMILARES
    '=============================
    Function ObtenerBaseRegla2(nombre As String) As String
        Dim partes() As String = nombre.Split("_")
        If partes.Length < 10 Then Return nombre
        Return String.Join("_", partes.Take(partes.Length - 3))
    End Function

    Function YaExisteSimilar(nombreNuevo As String, carpeta As String) As Boolean
        Dim baseNuevo = ObtenerBaseRegla2(nombreNuevo)
        For Each archivo In Directory.GetFiles(carpeta, "*.pdf")
            Dim nombreExistente = Path.GetFileNameWithoutExtension(archivo)
            If ObtenerBaseRegla2(nombreExistente) = baseNuevo Then
                Return True
            End If
        Next
        Return False
    End Function

#End Region

#Region "File Operations (Rename, Duplicate, Delete)"
    '=============================
    ' RENOMBRAR (con doble captura)
    '=============================
    Private Sub btnRenombrar_Click(sender As Object, e As EventArgs) Handles btnRenombrar.Click

        If rutaArchivo = "" Then Exit Sub

        Dim errorMsg As String = ""

        If Not ValidarNombreGeneral(txtNombre.Text, errorMsg) Then
            MessageBox.Show(errorMsg)
            Exit Sub
        End If

        ' ===== PRIMERA CAPTURA =====
        If Not esperandoConfirmacion Then
            capturaPendiente = txtNombre.Text.Trim()
            esperandoConfirmacion = True
            txtNombre.Clear()
            txtNombre.BackColor = Color.White
            txtNombre.Focus()
            lblCaptura.Text = "Confirme el nombre"
            lblCaptura.ForeColor = Color.DarkOrange
            lblEstado.Text = "Esperando confirmación..."
            Exit Sub
        End If

        ' ===== SEGUNDA CAPTURA =====
        If txtNombre.Text.Trim() <> capturaPendiente Then
            MessageBox.Show("Los nombres no coinciden. Se reiniciará la captura.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            ReiniciarCaptura()
            txtNombre.Focus()
            Exit Sub
        End If

        ' Coinciden — proceder con el renombrado
        Dim carpeta = Path.GetDirectoryName(rutaArchivo)
        Dim nuevaRuta = Path.Combine(carpeta, capturaPendiente & ".pdf")

        If File.Exists(nuevaRuta) Then
            MessageBox.Show("Ya existe un archivo con ese nombre")
            ReiniciarCaptura()
            Exit Sub
        End If

        ' Detección de similar
        If YaExisteSimilar(capturaPendiente, carpeta) Then
            Dim resp = MessageBox.Show("⚠️ Ya existe un documento similar ¿Continuar?", "Advertencia", MessageBoxButtons.YesNo)
            If resp = DialogResult.No Then
                ReiniciarCaptura()
                Exit Sub
            End If
        End If

        WebView21.CoreWebView2.Navigate("about:blank")
        Application.DoEvents()

        Dim nombreOriginal = Path.GetFileName(rutaArchivo)
        File.Move(rutaArchivo, nuevaRuta)

        rutaArchivo = nuevaRuta
        listaPDFs(indiceActual) = nuevaRuta

        totalRenombrados += 1
        ActualizarContadores()

        ' Registrar auditoría
        ModuloAuditoria.RegistrarAccion("Renombrar", nombreOriginal, Path.GetFileName(nuevaRuta))

        ' Marcar como renombrado, NO avanza automáticamente
        pdfFueRenombrado = True
        esperandoConfirmacion = False
        capturaPendiente = ""
        txtNombre.Clear()
        lblCaptura.Text = "Renombrado ✅"
        lblCaptura.ForeColor = Color.Green
        lblNombreActual.Text = "Nombre actual: " & Path.GetFileName(nuevaRuta)

        ' Recargar el PDF renombrado con delay para que WebView2 lo reconozca
        System.Threading.Thread.Sleep(300)
        Application.DoEvents()
        MostrarPDF(rutaArchivo)

    End Sub

    '=============================
    ' DUPLICAR (captura simple)
    '=============================
    Private Sub btnDuplicar_Click(sender As Object, e As EventArgs) Handles btnDuplicar.Click

        If rutaArchivo = "" Then Exit Sub

        Dim errorMsg As String = ""

        If Not ValidarNombreGeneral(txtNombre.Text, errorMsg) Then
            MessageBox.Show(errorMsg)
            Exit Sub
        End If

        Dim carpeta = Path.GetDirectoryName(rutaArchivo)
        Dim nuevaRuta = Path.Combine(carpeta, txtNombre.Text & ".pdf")

        If File.Exists(nuevaRuta) Then
            MessageBox.Show("Ya existe un archivo con ese nombre")
            Exit Sub
        End If

        ' Detección de similar
        If YaExisteSimilar(txtNombre.Text, carpeta) Then
            Dim resp = MessageBox.Show("⚠️ Ya existe un documento similar ¿Continuar?", "Advertencia", MessageBoxButtons.YesNo)
            If resp = DialogResult.No Then Exit Sub
        End If

        File.Copy(rutaArchivo, nuevaRuta)

        totalDuplicados += 1
        ActualizarContadores()

        ' Registrar auditoría
        ModuloAuditoria.RegistrarAccion("Duplicar", Path.GetFileName(rutaArchivo), Path.GetFileName(nuevaRuta))

        lblEstado.Text = "Copia creada"
        txtNombre.Clear()
        txtNombre.Focus()

    End Sub

    '=============================
    ' ELIMINAR
    '=============================
    Private Sub btnEliminar_Click(sender As Object, e As EventArgs) Handles btnEliminar.Click

        If rutaArchivo = "" Then Exit Sub

        Dim resp = MessageBox.Show("¿Seguro que deseas eliminar este PDF?",
                               "Confirmar eliminación",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Warning)

        If resp = DialogResult.No Then Exit Sub

        Try
            Dim nombreEliminado = Path.GetFileName(rutaArchivo)

            WebView21.CoreWebView2.Navigate("about:blank")
            Application.DoEvents()

            File.Delete(rutaArchivo)

            totalEliminados += 1
            ActualizarContadores()

            ' Registrar auditoría
            ModuloAuditoria.RegistrarAccion("Eliminar", nombreEliminado, "", "Archivo eliminado permanentemente")

            listaPDFs.RemoveAt(indiceActual)

            If indiceActual >= listaPDFs.Count Then
                indiceActual = listaPDFs.Count - 1
            End If

            ReiniciarCaptura()

            If listaPDFs.Count > 0 Then
                CargarPDFActual()
            Else
                MessageBox.Show("Ya no hay PDFs")
                rutaArchivo = ""
                lblNombreActual.Text = "Sin archivos"
                lblTotalPdf.Text = ""
            End If

        Catch ex As Exception
            MessageBox.Show("Error al eliminar: " & ex.Message)
        End Try

    End Sub

#End Region

#Region "Page Rotation"
    '=============================
    ' ROTAR PÁGINA
    '=============================
    Private Sub btnRotarDerecha_Click(sender As Object, e As EventArgs) Handles btnRotarDerecha.Click
        RotarPagina(90)
    End Sub

    Private Sub btnRotarIzquierda_Click(sender As Object, e As EventArgs) Handles btnRotarIzquierda.Click
        RotarPagina(-90)
    End Sub

    Private Sub RotarPagina(grados As Integer)
        If rutaArchivo = "" Then Exit Sub

        Try
            Dim numPagina = CInt(nudPagina.Value)

            ' Navegar a blank y esperar para liberar el archivo
            WebView21.CoreWebView2.Navigate("about:blank")
            Application.DoEvents()
            System.Threading.Thread.Sleep(500)
            Application.DoEvents()

            ' Leer en memoria para poder sobreescribir
            Dim bytes = File.ReadAllBytes(rutaArchivo)

            ' Guardar a un archivo temporal, luego reemplazar
            Dim archivoTemp = rutaArchivo & ".tmp"

            Using ms As New MemoryStream(bytes)
                Using reader As New PdfReader(ms)
                    Using writer As New PdfWriter(archivoTemp)
                        Using doc As New PdfDocument(reader, writer)
                            If numPagina < 1 OrElse numPagina > doc.GetNumberOfPages() Then
                                MessageBox.Show("Número de página inválido")
                                MostrarPDF(rutaArchivo)
                                Exit Sub
                            End If

                            Dim page = doc.GetPage(numPagina)
                            Dim rotacionActual = page.GetRotation()
                            page.SetRotation((rotacionActual + grados + 360) Mod 360)
                        End Using
                    End Using
                End Using
            End Using

            ' Reemplazar el original con el temporal
            File.Delete(rutaArchivo)
            File.Move(archivoTemp, rutaArchivo)

            ' Marcar que este PDF fue rotado (se registrará en auditoría al validar)
            pdfFueRotado = True
            Dim direccion = If(grados > 0, "derecha", "izquierda")

            ' Recargar PDF
            System.Threading.Thread.Sleep(200)
            MostrarPDF(rutaArchivo)
            lblEstado.Text = "Página " & numPagina & " rotada " & Math.Abs(grados) & "° a la " & direccion

        Catch ex As Exception
            MessageBox.Show("Error al rotar: " & ex.Message)
            ' Limpiar temporal si quedó
            Try
                Dim archivoTemp = rutaArchivo & ".tmp"
                If File.Exists(archivoTemp) Then File.Delete(archivoTemp)
            Catch
            End Try
            If rutaArchivo <> "" Then MostrarPDF(rutaArchivo)
        End Try
    End Sub

    '=============================
    ' CERRAR SESIÓN
    '=============================
    Private Sub btnCerrarSesion_Click(sender As Object, e As EventArgs) Handles btnCerrarSesion.Click
        Dim resp = MessageBox.Show("¿Desea cerrar sesión?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If resp = DialogResult.Yes Then
            ' Liberar lote actual
            ModuloDirectorios.LiberarLoteActual()

            ' Limpiar sesión de usuario
            ModuloUsuarios.UsuarioActual = Nothing
            ModuloUsuarios.SesionActual = ""

            ' Volver al login
            Dim frmLogin As New FormLogin()
            If frmLogin.ShowDialog() <> DialogResult.OK Then
                Application.Exit()
                Exit Sub
            End If

            ' Seleccionar nuevo directorio/lote
            Dim frmSesion As New FormSeleccionSesion()
            If frmSesion.ShowDialog() <> DialogResult.OK Then
                Application.Exit()
                Exit Sub
            End If

            ' Reiniciar todo
            Dim titulo = "Validación PDF — " & ModuloUsuarios.UsuarioActual.NombreCompleto
            titulo &= " | " & ModuloDirectorios.DirectorioActual.Nombre
            titulo &= " › " & ModuloDirectorios.LoteActual
            Me.Text = titulo
            totalRenombrados = 0
            totalDuplicados = 0
            totalEliminados = 0
            ActualizarContadores()
            rutaArchivo = ""
            listaPDFs = Nothing
            indiceActual = 0
            ReiniciarCaptura()
            lblNombreActual.Text = "Nombre actual del PDF"
            lblTotalPdf.Text = "Total de PDF"
            WebView21.CoreWebView2.Navigate("about:blank")
            CargarPDFsDesdeRuta(ModuloDirectorios.RutaLoteActual)
        End If
    End Sub

#End Region

End Class