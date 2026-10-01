Public Class FormUsuarios

    Private Sub FormUsuarios_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CargarTabla()
    End Sub

    Private Sub CargarTabla()
        Dim usuarios = ModuloUsuarios.CargarUsuarios()
        dgvUsuarios.DataSource = Nothing
        dgvUsuarios.Columns.Clear()
        Dim dt As New DataTable()
        dt.Columns.Add("ID", GetType(Integer))
        dt.Columns.Add("Nombre Completo", GetType(String))
        dt.Columns.Add("Usuario", GetType(String))
        For Each u In usuarios
            dt.Rows.Add(u.Id, u.NombreCompleto, u.NombreUsuario)
        Next
        dgvUsuarios.DataSource = dt
    End Sub

    Private Sub btnAgregar_Click(sender As Object, e As EventArgs) Handles btnAgregar.Click
        If String.IsNullOrWhiteSpace(txtNombreCompleto.Text) Then
            MessageBox.Show("Ingrese el nombre completo")
            Exit Sub
        End If
        If String.IsNullOrWhiteSpace(txtNombreUsuario.Text) Then
            MessageBox.Show("Ingrese el nombre de usuario")
            Exit Sub
        End If
        If txtPinUsuario.Text.Length <> 4 Then
            MessageBox.Show("El PIN debe ser de 4 dígitos")
            Exit Sub
        End If
        Dim usuarios = ModuloUsuarios.CargarUsuarios()
        If usuarios.Any(Function(u) u.NombreUsuario.ToLower() = txtNombreUsuario.Text.Trim().ToLower()) Then
            MessageBox.Show("Ese nombre de usuario ya existe")
            Exit Sub
        End If
        ModuloUsuarios.AgregarUsuario(txtNombreCompleto.Text.Trim(), txtNombreUsuario.Text.Trim(), txtPinUsuario.Text.Trim())
        txtNombreCompleto.Clear()
        txtNombreUsuario.Clear()
        txtPinUsuario.Clear()
        CargarTabla()
        MessageBox.Show("Usuario agregado correctamente")
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As EventArgs) Handles btnEliminar.Click
        If dgvUsuarios.SelectedRows.Count = 0 Then
            MessageBox.Show("Seleccione un usuario")
            Exit Sub
        End If
        Dim id = CInt(dgvUsuarios.SelectedRows(0).Cells("ID").Value)
        Dim nombre = dgvUsuarios.SelectedRows(0).Cells("Nombre Completo").Value.ToString()
        Dim resp = MessageBox.Show("¿Eliminar al usuario '" & nombre & "'?", "Confirmar", MessageBoxButtons.YesNo)
        If resp = DialogResult.Yes Then
            ModuloUsuarios.EliminarUsuario(id)
            CargarTabla()
        End If
    End Sub

    Private Sub btnCerrar_Click(sender As Object, e As EventArgs) Handles btnCerrar.Click
        Me.Close()
    End Sub

    Private Sub txtPinUsuario_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtPinUsuario.KeyPress
        If Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> ControlChars.Back Then
            e.Handled = True
        End If
    End Sub

End Class
