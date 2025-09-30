using System.Net;

namespace VidaUtilCF.View;

public partial class Principal : ContentPage
{
    public Principal()
    {
        InitializeComponent();
        fechaActualLabel.Text = "Fecha Actual: " + DateTime.Now.ToString("dd/MM/yyyy");

        // Asegurarse que el botón inicia desactivado
        reportarVidaUtilButton.IsEnabled = false;
    }

    private void CalcularVidaUtil(object sender, EventArgs e)
    {
        DateTime fechaElaboracion;
        DateTime fechaCaducidad = fechaCaducidadPicker.Date;
        DateTime fechaActual = DateTime.Now.Date; // Solo fecha sin hora

        if (usarDiaJulianoCheckBox.IsChecked)
        {
            // Obtener el día juliano ingresado
            if (int.TryParse(codigoJulianoEntry.Text, out int diaJuliano) && diaJuliano >= 1 && diaJuliano <= 365)
            {
                // Convertir día juliano a fecha normal usando el año actual
                fechaElaboracion = ConvertirDiaJulianoAFecha(diaJuliano, DateTime.Now.Year);
            }
            else
            {
                errorLabel.Text = "Ingrese un día juliano válido (1-365).";
                reportarVidaUtilButton.IsEnabled = false;
                return;
            }
        }
        else
        {
            fechaElaboracion = fechaElaboracionPicker.Date;
        }

        int diasTranscurridos = (fechaActual - fechaElaboracion).Days;
        int diasRestantes = (fechaCaducidad - fechaActual).Days;
        int totalVidaUtil = (fechaCaducidad - fechaElaboracion).Days;

        var (años, meses, días) = DescomponerDias(totalVidaUtil);

        diasRestantesLabel.Text = $"Días restantes: {diasRestantes}";
        diasTranscurridosLabel.Text = $"Días transcurridos: {diasTranscurridos}";
        diasTotalesLabel.Text = $"Días Totales: {totalVidaUtil} (Años: {años}, Meses: {meses}, Días: {días})";

        errorLabel.Text = "";

        bool esImportado = productoImportadoCheckBox.IsChecked;

        double porcentajeVidaUtil = totalVidaUtil > 0
            ? (double)diasRestantes / totalVidaUtil * 100
            : 0;

        porcentajeVidaUtilLabel.Text = $"Porcentaje de vida útil: {porcentajeVidaUtil:0.00}%";

        string clasificacion = ClasificarProducto(totalVidaUtil, porcentajeVidaUtil, esImportado);
        clasificacionLabel.Text = clasificacion;

        // Variable para controlar activación del botón
        bool noCumple = false;

        if (esImportado)
        {
            if (porcentajeVidaUtil < 50)
            {
                errorLabel.Text = "El producto importado no cumple con el 50% de vida útil.";
                noCumple = true;
            }
        }
        else
        {
            if (totalVidaUtil <= 90 && porcentajeVidaUtil <= 80) noCumple = true;
            else if (totalVidaUtil > 90 && totalVidaUtil <= 180 && porcentajeVidaUtil <= 80) noCumple = true;
            else if (totalVidaUtil > 180 && totalVidaUtil <= 365 && porcentajeVidaUtil <= 70) noCumple = true;
            else if (totalVidaUtil > 365 && porcentajeVidaUtil <= 60) noCumple = true;
        }

        reportarVidaUtilButton.IsEnabled = noCumple;
    }

    // Método auxiliar para convertir día juliano (1-365) a fecha normal
    private DateTime ConvertirDiaJulianoAFecha(int diaJuliano, int año)
    {
        // Asumimos que no es año bisiesto, si quieres, puedes agregar lógica para ello
        DateTime primerDia = new DateTime(año, 1, 1);
        return primerDia.AddDays(diaJuliano - 1);
    }

    private (int años, int meses, int días) DescomponerDias(int totalDias)
    {
        int años = totalDias / 365;
        totalDias %= 365;

        int meses = totalDias / 30;
        totalDias %= 30;

        int días = totalDias;

        return (años, meses, días);
    }

    private string ClasificarProducto(int totalVidaUtil, double porcentajeVidaUtil, bool esImportado)
    {
        if (esImportado)
        {
            return "Clasificación: Producto Importado";
        }
        else
        {
            if (totalVidaUtil <= 90)
            {
                if (porcentajeVidaUtil > 80)
                    return "Clasificación: Vida Corta";
                else
                    errorLabel.Text = "El producto no cumple con el 80% requerido para Vida Corta.";
            }
            else if (totalVidaUtil > 90 && totalVidaUtil <= 180)
            {
                if (porcentajeVidaUtil > 80)
                    return "Clasificación: Vida Media";
                else
                    errorLabel.Text = "El producto no cumple con el 80% requerido para Vida Media.";
            }
            else if (totalVidaUtil > 180 && totalVidaUtil <= 365)
            {
                if (porcentajeVidaUtil > 70)
                    return "Clasificación: Vida Larga";
                else
                    errorLabel.Text = "El producto no cumple con el 70% requerido para Vida Larga.";
            }
            else if (totalVidaUtil > 365)
            {
                if (porcentajeVidaUtil > 60)
                    return "Clasificación: Vida Muy Larga";
                else
                    errorLabel.Text = "El producto no cumple con el 60% requerido para Vida Muy Larga.";
            }

            return "Clasificación: Baja Vida Útil";
        }
    }

    private async void ReportarVidaUtil(object sender, EventArgs e)
    {
        // Pedir código del proveedor
        string codigoProveedor = await DisplayPromptAsync("Código Proveedor", "Ingrese el código del proveedor:");

        if (codigoProveedor == null) // Usuario canceló
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(codigoProveedor))
        {
            await DisplayAlert("Aviso", "Debe ingresar el código del proveedor.", "OK");
            return;
        }

        // Pedir número de cajas o packs
        string numeroCajasStr = await DisplayPromptAsync("Número de cajas/packs", "Ingrese la cantidad de cajas o packs:");

        if (numeroCajasStr == null) // Usuario canceló
        {
            return;
        }

        if (!int.TryParse(numeroCajasStr, out int numeroCajas) || numeroCajas <= 0)
        {
            await DisplayAlert("Aviso", "Debe ingresar un número válido de cajas o packs.", "OK");
            return;
        }

        // Obtener valores desde los Labels existentes
        string porcentajeTexto = porcentajeVidaUtilLabel.Text.Replace("Porcentaje de vida útil: ", "").Replace("%", "").Trim();
        double.TryParse(porcentajeTexto, out double porcentajeActual);

        string clasificacion = clasificacionLabel.Text;
        string mensajeFinal = "";

        // Calcular mínimo requerido
        double requerido = 0;
        int totalDias = int.Parse(System.Text.RegularExpressions.Regex.Match(diasTotalesLabel.Text, @"\d+").Value);
        bool esImportado = productoImportadoCheckBox.IsChecked;

        if (esImportado)
        {
            requerido = 50;
        }
        else
        {
            if (totalDias <= 90)
                requerido = 80;
            else if (totalDias <= 180)
                requerido = 80;
            else if (totalDias <= 365)
                requerido = 70;
            else
                requerido = 60;
        }

        if (porcentajeActual < requerido)
        {
            double diferencia = requerido - porcentajeActual;
            mensajeFinal = $"⚠️ El producto NO cumple con el mínimo requerido.\n" +
                           $"Porcentaje actual: {porcentajeActual:0.00}%\n" +
                           $"Porcentaje requerido: {requerido}%\n";
        }
        else
        {
            mensajeFinal = $"El producto CUMPLE con los requisitos.\n" +
                           $"Porcentaje actual: {porcentajeActual:0.00}%\n" +
                           $"Porcentaje requerido: {requerido}%";
        }

        // Construir mensaje completo para WhatsApp
        string mensaje = $"❗ *Reporte de Vida Útil* ❗\n" +
                         $"🚚 Código Proveedor: {codigoProveedor}\n" +
                         $"📦 Número de cajas/packs: {numeroCajas}\n" +
                         $"{mensajeFinal}";

        string mensajeUrl = WebUtility.UrlEncode(mensaje);
        string whatsappUrl = $"https://api.whatsapp.com/send?text={mensajeUrl}";

        try
        {
            await Launcher.OpenAsync(whatsappUrl);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo abrir WhatsApp: " + ex.Message, "OK");
        }
    }

}

