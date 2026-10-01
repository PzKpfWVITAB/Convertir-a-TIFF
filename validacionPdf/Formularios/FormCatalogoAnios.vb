Public Class FormCatalogoAnios

    Private Sub FormCatalogoAnios_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ModuloCatalogo.InicializarCatalogo()
        CargarLista()
    End Sub

    Private Sub CargarLista()
        lstAnios.Items.Clear()
        Dim anios = ModuloCatalogo.CargarAnios()
        For Each a In anios
            lstAnios.Items.Add(a)
        Next
    End Sub

    Private Sub btnAgregar_Click(sender As Object, e As EventArgs) Handles btnAgregar.Click
        Dim anio = CInt(nudAnio.Value)
        If ModuloCatalogo.AgregarAnio(anio) Then
            CargarLista()
        Else
            MessageBox.Show("El año " & anio & " ya existe en el catálogo")
        End If
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As EventArgs) Handles btnEliminar.Click
        If lstAnios.SelectedItem Is Nothing Then
            MessageBox.Show("Seleccione un año para eliminar")
            Exit Sub
        End If
        Dim anio = CInt(lstAnios.SelectedItem)
        Dim resp = MessageBox.Show("¿Eliminar el año " & anio & "?", "Confirmar", MessageBoxButtons.YesNo)
        If resp = DialogResult.Yes Then
            ModuloCatalogo.EliminarAnio(anio)
            CargarLista()
        End If
    End Sub

    Private Sub btnCerrar_Click(sender As Object, e As EventArgs) Handles btnCerrar.Click
        Me.Close()
    End Sub

End Class
