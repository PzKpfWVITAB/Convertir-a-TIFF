Public Class FormSeleccionSesion

    Private directorios As List(Of ModuloDirectorios.DirectorioInfo)
    Private delegaciones As List(Of ModuloDirectorios.Delegacion)
    Private delegacionSeleccionada As ModuloDirectorios.Delegacion = Nothing

    ' ===== CARGA =====

    Private Sub FormSeleccionSesion_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        lblUsuario.Text = "Usuario: " & ModuloUsuarios.UsuarioActual.NombreCompleto
        CargarDirectorios()
        CargarDelegaciones()
    End Sub

    Private Sub CargarDelegaciones()
        delegaciones = ModuloDirectorios.CargarDelegaciones()
    End Sub

    Private Sub CargarDirectorios()
        directorios = ModuloDirectorios.CargarDirectorios()
        lvwDirectorios.Items.Clear()

        For Each dir As ModuloDirectorios.DirectorioInfo In directorios
            Dim conectado = ModuloDirectorios.DirectorioConectado(dir.Ruta)
            Dim item = lvwDirectorios.Items.Add(dir.Nombre)
            item.SubItems.Add(If(conectado, "✔ Conectado", "✘ Sin conexión"))
            item.ForeColor = If(conectado, Color.DarkGreen, Color.Firebrick)
            item.Tag = dir
        Next
    End Sub

    ' ===== SELECCIÓN DE DIRECTORIO =====

    Private Sub lvwDirectorios_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles lvwDirectorios.SelectedIndexChanged

        If lvwDirectorios.SelectedItems.Count = 0 Then Exit Sub

        Dim dir = CType(lvwDirectorios.SelectedItems(0).Tag, ModuloDirectorios.DirectorioInfo)

        If Not ModuloDirectorios.DirectorioConectado(dir.Ruta) Then
            lblEstadoLotes.Text = "Sin conexión al directorio seleccionado."
            lvwDelegaciones.Items.Clear()
            btnComenzar.Enabled = False
            Exit Sub
        End If

        ' Mostrar delegaciones
        lvwDelegaciones.Items.Clear()
        delegacionSeleccionada = Nothing
        btnComenzar.Enabled = False

        For Each del As ModuloDirectorios.Delegacion In delegaciones
            Dim item = lvwDelegaciones.Items.Add(del.Id)
            item.SubItems.Add(del.NombreCompleto)
            item.Tag = del
        Next

        lblEstadoLotes.Text = "Seleccione una delegación."

    End Sub

    ' ===== SELECCIÓN DE DELEGACIÓN =====

    Private Sub lvwDelegaciones_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles lvwDelegaciones.SelectedIndexChanged

        If lvwDelegaciones.SelectedItems.Count = 0 Then
            btnComenzar.Enabled = False
            Exit Sub
        End If

        delegacionSeleccionada = CType(lvwDelegaciones.SelectedItems(0).Tag, ModuloDirectorios.Delegacion)
        lblEstadoLotes.Text = "Delegación seleccionada: " & delegacionSeleccionada.NombreCompleto & ". Al comenzar, se le asignará un lote disponible automáticamente."
        btnComenzar.Enabled = True

    End Sub

    ' ===== COMENZAR =====

    Private Async Sub btnComenzar_Click(sender As Object, e As EventArgs) Handles btnComenzar.Click

        If delegacionSeleccionada Is Nothing Then Exit Sub
        If lvwDirectorios.SelectedItems.Count = 0 Then Exit Sub

        Dim dir = CType(lvwDirectorios.SelectedItems(0).Tag, ModuloDirectorios.DirectorioInfo)

        ' Buscar lote libre automáticamente (en segundo plano)
        Dim frmCarga As New FormCargando()
        frmCarga.CambiarMensaje("Buscando lote libre en la red...")
        frmCarga.Show(Me)
        Application.DoEvents()

        btnComenzar.Enabled = False
        btnRefrescar.Enabled = False
        lvwDirectorios.Enabled = False
        lvwDelegaciones.Enabled = False

        Dim loteLibre As ModuloDirectorios.LoteInfo = Nothing
        Try
            loteLibre = Await Task.Run(Function() ModuloDirectorios.BuscarLoteLibre(dir, delegacionSeleccionada))
        Catch ex As Exception
            MessageBox.Show("Ocurrió un error al escanear la red: " & ex.Message)
        End Try

        frmCarga.Close()
        frmCarga.Dispose()

        btnComenzar.Enabled = True
        btnRefrescar.Enabled = True
        lvwDirectorios.Enabled = True
        lvwDelegaciones.Enabled = True

        If loteLibre Is Nothing Then
            MessageBox.Show(
                "No hay lotes disponibles para la delegación '" & delegacionSeleccionada.NombreCompleto & "'. Todos están ocupados o no existen.",
                "Sin lotes disponibles", MessageBoxButtons.OK, MessageBoxIcon.Information)
            lblEstadoLotes.Text = "Seleccione otra delegación."
            Exit Sub
        End If

        ' Guardar estado global
        ModuloDirectorios.DirectorioActual = dir
        ModuloDirectorios.DelegacionActual = delegacionSeleccionada
        ModuloDirectorios.LoteActual = loteLibre.NombreLote
        ModuloDirectorios.RutaLoteActual = loteLibre.RutaCompleta

        ' Crear carpeta Entregable/{id}
        ModuloDirectorios.InicializarRutaEntregable()

        ' Tomar el lote (.lock)
        ModuloDirectorios.TomarLote(loteLibre.RutaCompleta)

        Me.DialogResult = DialogResult.OK
        Me.Close()

    End Sub

    ' ===== CANCELAR =====

    Private Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    ' ===== REFRESCAR =====

    Private Sub btnRefrescar_Click(sender As Object, e As EventArgs) Handles btnRefrescar.Click
        CargarDirectorios()
        lvwDelegaciones.Items.Clear()
        lblEstadoLotes.Text = "Seleccione un directorio."
        btnComenzar.Enabled = False
        delegacionSeleccionada = Nothing
    End Sub

End Class
