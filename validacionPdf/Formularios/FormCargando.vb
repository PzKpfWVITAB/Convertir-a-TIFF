Public Class FormCargando
    Private angulo As Integer = 0

    Public Sub CambiarMensaje(texto As String)
        lblMensaje.Text = texto
    End Sub

    Private Sub picCarga_Paint(sender As Object, e As PaintEventArgs) Handles picCarga.Paint
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        
        ' Dibujar círculo de fondo gris claro
        Dim penFondo As New Pen(Color.FromArgb(230, 230, 230), 8)
        e.Graphics.DrawEllipse(penFondo, 10, 10, 60, 60)
        
        ' Dibujar arco de carga Teal
        Dim penCarga As New Pen(Color.Teal, 8)
        penCarga.StartCap = Drawing2D.LineCap.Round
        penCarga.EndCap = Drawing2D.LineCap.Round
        e.Graphics.DrawArc(penCarga, 10, 10, 60, 60, angulo, 120)
    End Sub

    Private Sub timerCarga_Tick(sender As Object, e As EventArgs) Handles timerCarga.Tick
        angulo = (angulo + 15) Mod 360
        picCarga.Invalidate()
    End Sub
End Class
