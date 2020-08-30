

using Android.Text;
using System.Collections.Generic;
using Xamarin.Forms;

namespace AyudanteNewen.Clases
{
    static class Auxiliar
    {
        public static List<DetallePedido> ParsearJsonDetallePedido(string entradaJson)
        {
            var DetallePedido = new List<DetallePedido>();
            entradaJson = entradaJson.Replace("{[{", "").Replace("}]}", "").Replace("},", "");
            string[] objetosJson = entradaJson.Split('{');
            string[] propiedades;
            bool esImpar = true;
            foreach(var objetoJson in objetosJson)
            {
                propiedades = objetoJson.Replace(", ", "|").Split('|');
                var lineaDetalle = new DetallePedido();
                string[] campoValor;
                foreach(var propiedad in propiedades)
                {
                    campoValor = propiedad.Split(':');
                    switch (campoValor[0].Trim()) {
                        case "idProducto":
                            lineaDetalle.IdProducto = campoValor[1].Trim();
                            break;
                        case "nombreProducto":
                            lineaDetalle.NombreProducto = campoValor[1].Trim();
                            break;
                        case "cantidad":
                            lineaDetalle.Cantidad = campoValor[1].Trim();
                            break;
                        case "precio":
                            lineaDetalle.Precio = campoValor[1].Trim();
                            break;
                    }
                }
                lineaDetalle.ColorFondo = esImpar ? Color.FromHex("#E2E2E1") : Color.FromHex("#EDEDED");
                DetallePedido.Add(lineaDetalle);
                esImpar = !esImpar;
            }
            return DetallePedido;
        }
    }
}
