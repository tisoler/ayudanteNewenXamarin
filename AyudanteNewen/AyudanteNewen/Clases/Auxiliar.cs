

using Android.Text;
using System;
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
                var valor = "";
                foreach(var propiedad in propiedades)
                {
                    campoValor = propiedad?.Split(':');
                    valor = campoValor[1]?.Trim().Trim('\'');
                    switch (campoValor[0].Trim()) {
                        case "idProducto":
                            lineaDetalle.IdProducto = valor;
                            break;
                        case "nombreProducto":
                            lineaDetalle.NombreProducto = valor;
                            break;
                        case "cantidad":
                            lineaDetalle.Cantidad = valor;
                            break;
                        case "precio":
                            lineaDetalle.Precio = valor;
                            break;
                        case "columnaStockElegido":
                            lineaDetalle.ColumnaStockElegido = Convert.ToUInt32(valor);
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
