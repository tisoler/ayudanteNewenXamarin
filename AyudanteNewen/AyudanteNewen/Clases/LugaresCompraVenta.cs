
using Google.GData.Spreadsheets;
using AyudanteNewen.Servicios;

namespace AyudanteNewen.Clases
{
	public class LugaresCompraVenta
	{
		private CellFeed _celdas;

		public void ObtenerActualizarLugares(string linkHojaPrincipal, SpreadsheetsService servicio)
		{
			//Recibe el link de la hoja principal, obtiene el link de lugares (si tiene) para la hoja actual y lo actualiza (por si se agregaron lugares)
			var paginaLugares = CuentaUsuario.RecuperarValorDeCuentaLocal(linkHojaPrincipal + "|hojaPuntosVenta");
			var puntosVentaTexto = "";

			if (paginaLugares != null)
			{
				_celdas = new ServiciosGoogle().ObtenerCeldasDeUnaHoja(paginaLugares, servicio);

				foreach (CellEntry celda in _celdas.Entries)
				{
					if (celda.Row != 1)
						puntosVentaTexto += celda.Value + "|";
				}
			}

			CuentaUsuario.AlmacenarPuntosVenta(puntosVentaTexto.TrimEnd('|'));
		}
	}
}
