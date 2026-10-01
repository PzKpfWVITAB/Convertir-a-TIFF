Public Class FormLogin

    Public Sub New()
        ModuloDiagnostico.Log("  -> FormLogin.New() iniciado")
        InitializeComponent()
        ModuloDiagnostico.Log("  -> FormLogin.New() InitializeComponent finalizado")
    End Sub

    Private Sub FormLogin_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ModuloDiagnostico.Log("  -> FormLogin_Load iniciado")
        Cursor.Current = Cursors.Default
        Me.Cursor = Cursors.Default
        ModuloUsuarios.InicializarDatos()
        txtUsuario.Focus()
        ModuloDiagnostico.Log("  -> FormLogin_Load finalizado con éxito")
    End Sub

    Private Sub FormLogin_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        ModuloDiagnostico.Log("  -> FormLogin_Shown: Formulario visible y recibiendo foco")
        Cursor.Current = Cursors.Default
        Me.Cursor = Cursors.Default
        Me.Activate()
        Me.BringToFront()
    End Sub

    Private Sub btnIniciar_Click(sender As Object, e As EventArgs) Handles btnIniciar.Click

        If String.IsNullOrWhiteSpace(txtUsuario.Text) OrElse String.IsNullOrWhiteSpace(txtPin.Text) Then
            lblError.Text = "Ingrese usuario y PIN"
            Exit Sub
        End If

        Dim usuario = ModuloUsuarios.ValidarLogin(txtUsuario.Text.Trim(), txtPin.Text.Trim())

        If usuario Is Nothing Then
            lblError.Text = "Usuario o PIN incorrecto"
            txtPin.Clear()
            txtPin.Focus()
            Exit Sub
        End If

        ' Iniciar sesión
        ModuloUsuarios.IniciarSesion(usuario)

        ' Abrir flujo tradicional de validación
        Me.Hide()
        Using frmSesion As New FormSeleccionSesion()
            If frmSesion.ShowDialog() = DialogResult.OK Then
                Using frm1 As New Form1()
                    frm1.ShowDialog()
                End Using
            End If
        End Using
        Me.Show()
        Me.Activate()
        txtPin.Clear()
        txtPin.Focus()

    End Sub

    ' Manejador común para validar PIN maestro
    Private Function ValidarAdmin() As Boolean
        Dim pinIngresado = InputBox("Ingrese el PIN maestro para acceder a esta opción:", "Autenticación")
        If String.IsNullOrEmpty(pinIngresado) Then Return False
        If pinIngresado <> ModuloUsuarios.ObtenerPinMaestro() Then
            MessageBox.Show("PIN maestro incorrecto", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End If
        Return True
    End Function

    Private Sub itemUsuarios_Click(sender As Object, e As EventArgs) Handles itemUsuarios.Click
        If ValidarAdmin() Then
            Dim frm As New FormUsuarios()
            frm.ShowDialog()
        End If
    End Sub

    Private Sub itemAnios_Click(sender As Object, e As EventArgs) Handles itemAnios.Click
        If ValidarAdmin() Then
            Dim frm As New FormCatalogoAnios()
            frm.ShowDialog()
        End If
    End Sub

    Private Sub itemDelegaciones_Click(sender As Object, e As EventArgs) Handles itemDelegaciones.Click
        If ValidarAdmin() Then
            Try
                Dim rutaJson = IO.Path.Combine(Application.StartupPath, "data", "delegaciones.json")
                If IO.File.Exists(rutaJson) Then
                    Process.Start(rutaJson)
                Else
                    MessageBox.Show("El archivo de delegaciones no existe.")
                End If
            Catch ex As Exception
                MessageBox.Show("Error al abrir JSON: " & ex.Message)
            End Try
        End If
    End Sub

    Private Sub itemAuditoria_Click(sender As Object, e As EventArgs) Handles itemAuditoria.Click
        If ValidarAdmin() Then
            Try
                Dim rutaJson = IO.Path.Combine(Application.StartupPath, "data", "auditoria.json")
                If IO.File.Exists(rutaJson) Then
                    Process.Start(rutaJson)
                Else
                    MessageBox.Show("El archivo de auditoría aún no ha sido creado.")
                End If
            Catch ex As Exception
                MessageBox.Show("Error al abrir JSON: " & ex.Message)
            End Try
        End If
    End Sub

    Private Sub itemRecursos_Click(sender As Object, e As EventArgs) Handles itemRecursos.Click
        If ValidarAdmin() Then
            Dim frm As New FormConfigRecursos()
            frm.ShowDialog()
        End If
    End Sub

    Private Sub txtPin_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtPin.KeyPress
        ' Solo permitir números en el PIN
        If Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> ControlChars.Back Then
            e.Handled = True
        End If
    End Sub

    Private Sub ButtonLotes_Click(sender As Object, e As EventArgs) Handles ButtonLotes.Click

        If String.IsNullOrWhiteSpace(txtUsuario.Text) OrElse String.IsNullOrWhiteSpace(txtPin.Text) Then
            lblError.Text = "Ingrese usuario y PIN"
            Exit Sub
        End If

        Dim usuario = ModuloUsuarios.ValidarLogin(txtUsuario.Text.Trim(), txtPin.Text.Trim())

        If usuario Is Nothing Then
            lblError.Text = "Usuario o PIN incorrecto"
            txtPin.Clear()
            txtPin.Focus()
            Exit Sub
        End If

        ModuloUsuarios.IniciarSesion(usuario)

        If usuario.NombreUsuario.ToLower() <> "admin" Then
            MessageBox.Show("Solo administradores pueden acceder a Lotes", "Acceso denegado")
            Exit Sub
        End If

        Me.Hide()
        Using frm As New FormLotes()
            frm.ShowDialog()
        End Using
        Me.Show()
        Me.Activate()
        txtPin.Clear()
        txtPin.Focus()

    End Sub

    Private Sub btnTransferirIrec_Click(sender As Object, e As EventArgs) Handles btnTransferirIrec.Click
        If String.IsNullOrWhiteSpace(txtUsuario.Text) OrElse String.IsNullOrWhiteSpace(txtPin.Text) Then
            lblError.Text = "Ingrese usuario y PIN"
            Exit Sub
        End If

        Dim usuario = ModuloUsuarios.ValidarLogin(txtUsuario.Text.Trim(), txtPin.Text.Trim())

        If usuario Is Nothing Then
            lblError.Text = "Usuario o PIN incorrecto"
            txtPin.Clear()
            txtPin.Focus()
            Exit Sub
        End If

        ModuloUsuarios.IniciarSesion(usuario)

        If usuario.NombreUsuario.ToLower() <> "admin" Then
            MessageBox.Show("Solo administradores pueden acceder a Transferir IREC", "Acceso denegado")
            Exit Sub
        End If

        Me.Hide()
        Using frm As New FormTransferir()
            frm.ShowDialog()
        End Using
        Me.Show()
        Me.Activate()
        txtPin.Clear()
        txtPin.Focus()

    End Sub
End Class
