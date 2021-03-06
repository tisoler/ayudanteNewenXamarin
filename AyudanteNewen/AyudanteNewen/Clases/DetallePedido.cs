﻿using Android.Media;
using Xamarin.Forms;

namespace AyudanteNewen.Clases
{
    public class DetallePedido
    {
        public DetallePedido(string idProducto, string nombreProducto, string cantidad, string precio, uint columnaStockElegido)
        {
            IdProducto = idProducto;
            NombreProducto = nombreProducto;
            Cantidad = cantidad;
            Precio = precio;
            ColumnaStockElegido = columnaStockElegido;
        }

        public DetallePedido()
        {
        }

        public string IdProducto { get; set; }
        public string NombreProducto { get; set; }
        public string Cantidad { get; set; }
        public string Precio { get; set; }
        public uint ColumnaStockElegido { get; set; }
        public Color ColorFondo { get; set; }
    }
}
