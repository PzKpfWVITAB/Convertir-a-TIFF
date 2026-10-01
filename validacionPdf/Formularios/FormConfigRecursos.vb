Imports System.IO

Public Class FormConfigRecursos

    Private Sub FormConfigRecursos_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ActualizarVistaHardware()
        CargarConfiguracion()
    End Sub

    Private Sub ActualizarVistaHardware()
        Dim info = ModuloRecursos.ObtenerInfoHardwareCompleta()

        lblCpu.Text = $"Procesador: {info.NombreCpu} ({info.NucleosLogicos} núcleos lógicos)"
        Dim motorStr As String
        If Not String.IsNullOrEmpty(info.RutaMuPdf) Then
            motorStr = $"MuPDF Extremo ({Path.GetFileName(info.RutaMuPdf)})"
        ElseIf Not String.IsNullOrEmpty(info.RutaGhostscript) Then
            motorStr = $"Ghostscript CLI ({Path.GetFileName(info.RutaGhostscript)})"
        Else
            motorStr = "Magick.NET Integrado"
        End If
        lblRecomendacion.Text = $"Cálculo Automático (90%): {info.HilosAsignados} Hilos CPU | {info.MemoriaAsignadaGB:0.0} GB RAM máx | Motor: {motorStr}"

        nudHilos.Maximum = info.NucleosLogicos
        nudMemoria.Maximum = CDec(Math.Max(1.0, info.TotalRamGB))
    End Sub

    Private Sub CargarConfiguracion()
        Dim cfg = ModuloRecursos.ObtenerConfiguracion()

        If cfg.ModoAutomatico Then
            rbAutomatico.Checked = True
            pnlManual.Enabled = False
        Else
            rbManual.Checked = True
            pnlManual.Enabled = True
        End If

        nudHilos.Value = Math.Max(nudHilos.Minimum, Math.Min(nudHilos.Maximum, cfg.HilosCpuManual))
        nudMemoria.Value = Math.Max(nudMemoria.Minimum, Math.Min(nudMemoria.Maximum, CDec(cfg.MemoriaRamManualGB)))
        txtStaging.Text = cfg.RutaStagingLocal
    End Sub

    Private Sub rbAutomatico_CheckedChanged(sender As Object, e As EventArgs) Handles rbAutomatico.CheckedChanged
        pnlManual.Enabled = rbManual.Checked
    End Sub

    Private Sub rbManual_CheckedChanged(sender As Object, e As EventArgs) Handles rbManual.CheckedChanged
        pnlManual.Enabled = rbManual.Checked
    End Sub

    Private Sub btnExaminar_Click(sender As Object, e As EventArgs) Handles btnExaminar.Click
        If Directory.Exists(txtStaging.Text) Then
            fbdStaging.SelectedPath = txtStaging.Text
        End If

        If fbdStaging.ShowDialog() = DialogResult.OK Then
            txtStaging.Text = fbdStaging.SelectedPath
        End If
    End Sub

    Private Sub btnRestablecer_Click(sender As Object, e As EventArgs) Handles btnRestablecer.Click
        Dim info = ModuloRecursos.ObtenerInfoHardwareCompleta()
        rbAutomatico.Checked = True
        nudHilos.Value = info.HilosAsignados
        nudMemoria.Value = CDec(Math.Max(1.0, info.MemoriaAsignadaGB))
        txtStaging.Text = "C:\MagickTempCache"

        Dim cfg As New ModuloRecursos.ConfiguracionRecursos With {
            .ModoAutomatico = True,
            .PorcentajeMargenLibre = 10,
            .HilosCpuManual = info.HilosAsignados,
            .MemoriaRamManualGB = CInt(Math.Floor(info.MemoriaAsignadaGB)),
            .RutaStagingLocal = txtStaging.Text.Trim(),
            .UsarBufferLocal = True
        }

        ModuloRecursos.GuardarConfiguracion(cfg)
        ActualizarVistaHardware()
        MessageBox.Show("Valores restablecidos al modo automático óptimo (90% CPU/RAM con 10% libre).", "Configuración Aplicada", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
        Try
            Dim cfg As New ModuloRecursos.ConfiguracionRecursos With {
                .ModoAutomatico = rbAutomatico.Checked,
                .PorcentajeMargenLibre = 10,
                .HilosCpuManual = CInt(nudHilos.Value),
                .MemoriaRamManualGB = CInt(nudMemoria.Value),
                .RutaStagingLocal = txtStaging.Text.Trim(),
                .UsarBufferLocal = True
            }

            If Not Directory.Exists(cfg.RutaStagingLocal) Then
                Try
                    Directory.CreateDirectory(cfg.RutaStagingLocal)
                Catch ex As Exception
                    MessageBox.Show("No se pudo crear la carpeta de búfer especificada: " & ex.Message & vbCrLf & "Se usará la carpeta temporal predeterminada.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End Try
            End If

            ModuloRecursos.GuardarConfiguracion(cfg)
            ActualizarVistaHardware()
            MessageBox.Show("Configuración guardada y aplicada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            MessageBox.Show("Error al guardar la configuración: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnCerrar_Click(sender As Object, e As EventArgs) Handles btnCerrar.Click
        Me.Close()
    End Sub

End Class
