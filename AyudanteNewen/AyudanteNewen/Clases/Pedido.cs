
using System.Collections.Generic;
using Xamarin.Forms;

namespace AyudanteNewen.Clases
{

	// Clase Pedido: utilizada para armar la lista scrolleable de pedidos
	[Android.Runtime.Preserve]
	public class Pedido
	{
		[Android.Runtime.Preserve]
		public Pedido(
			string idPedido,
			string fecha,
			string idCliente,
			string cliente,
			string detallePedido,
			string fechaEntrega,
			string estado,
			string usuario,
			string comentario,
			string lugar,
			int filaPlanillaCalculo
		)
		{
			Id = idPedido;
			Fecha = fecha;
			IdCliente = idCliente;
			Cliente = cliente;
			Detalle = Auxiliar.ParsearJsonDetallePedido(detallePedido);
			FechaEntrega = fechaEntrega;
			Estado = estado;
			Usuario = usuario;
			Comentario = comentario;
			Lugar = lugar;
			FilaPlanillaCalculo = filaPlanillaCalculo;
			ColorFondo = ("cancelado, finalizado").Contains(estado.ToLower())
				? Color.FromHex("#E2E2E1")
				: Color.FromHex("#32CEF9");
		}

		[Android.Runtime.Preserve]
		public string Id { get; }
		[Android.Runtime.Preserve]
		public string Fecha { get; }
		[Android.Runtime.Preserve]
		public string IdCliente { get; }
		[Android.Runtime.Preserve]
		public string Cliente { get; }
		[Android.Runtime.Preserve]
		public List<DetallePedido> Detalle { get; }
		[Android.Runtime.Preserve]
		public string FechaEntrega { get; }
		[Android.Runtime.Preserve]
		public string Estado { get; }
		[Android.Runtime.Preserve]
		public string Usuario { get; }
		[Android.Runtime.Preserve]
		public string Comentario { get; }

		[Android.Runtime.Preserve]
		public string Lugar { get; }
		[Android.Runtime.Preserve]
		public int FilaPlanillaCalculo { get; }
		[Android.Runtime.Preserve]
		public Color ColorFondo { get; }
	}
}
