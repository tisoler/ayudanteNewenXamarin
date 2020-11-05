
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Auth;

namespace AyudanteNewen.Clases
{
	public static class CuentaUsuario
	{
		private static Account _cuenta;

		private static void RecuperarCuentaLocal()
		{
			//Recupera la cuenta local
			if (_cuenta == null)
				_cuenta = AccountStore.Create().FindAccountsForService("AyudanteNewen").FirstOrDefault();
		}

		private static void GuardarValorEnCuentaLocal(string llave, string valor)
		{
			RecuperarCuentaLocal();

			if (_cuenta != null)
			{
				//Si existe la cuenta, agrega el valor a la cuenta
				_cuenta.Properties.Remove(llave);
				_cuenta.Properties.Add(llave, valor);
			}
			else
			{
				//Si no existe la cuenta, la crea y luego agrega el valor a la cuenta
				_cuenta = new Account { Username = "local" };
				_cuenta.Properties.Add(llave, valor);
			}

			//Almacena la cuenta local
			AccountStore.Create().Save(_cuenta, "AyudanteNewen");
		}

		internal static void RemoverValorEnCuentaLocal(string llave)
		{
			RecuperarCuentaLocal();

			if (_cuenta == null) return;
			_cuenta.Properties.Remove(llave); //Si existe la cuenta, remueve el valor de la cuenta
																				//Almacena la cuenta local
			AccountStore.Create().Save(_cuenta, "AyudanteNewen");
		}

		public static string RecuperarValorDeCuentaLocal(string llave)
		{
			RecuperarCuentaLocal();
			return (_cuenta != null && _cuenta.Properties.ContainsKey(llave)) ? _cuenta.Properties[llave] : null;
		}

		internal static void AlmacenarAccesoDatos(string acceso)
		{
			if (RecuperarValorDeCuentaLocal("accesoDatos") != acceso)
			{
				GuardarValorEnCuentaLocal("columnasParaVer", "");
				GuardarValorEnCuentaLocal("columnasInventario", "");
			}
			GuardarValorEnCuentaLocal("accesoDatos", acceso);
		}

		internal static void AlmacenarTokenDeGoogle(string tokenDeAcceso)
		{
			GuardarValorEnCuentaLocal("tokenDeGoogle", tokenDeAcceso);
		}

		internal static void AlmacenarFechaExpiracionToken(DateTime? fechaExpiracion)
		{
			GuardarValorEnCuentaLocal("fechaExpiracion", fechaExpiracion.ToString());
		}

		internal static void AlmacenarLinkHojaConsulta(string linkHojaConsulta)
		{
			GuardarValorEnCuentaLocal("linkHojaConsulta", linkHojaConsulta);
		}

		internal static void AlmacenarColumnasParaVerDeHoja(string linkHojaConsulta, string columnasParaVer)
		{
			GuardarValorEnCuentaLocal("columnasParaVer", columnasParaVer);
			GuardarValorEnCuentaLocal(linkHojaConsulta + "|ver", columnasParaVer);
		}

		internal static void AlmacenarColumnasParaVer(string columnasParaVer)
		{
			GuardarValorEnCuentaLocal("columnasParaVer", columnasParaVer);
		}

		internal static void AlmacenarColumnasInventarioDeHoja(string linkHojaConsulta, string columnasInventario)
		{
			GuardarValorEnCuentaLocal("columnasInventario", columnasInventario);
			GuardarValorEnCuentaLocal(linkHojaConsulta + "|inventario", columnasInventario);
		}

		internal static void AlmacenarColumnasInventario(string columnasInventario)
		{
			GuardarValorEnCuentaLocal("columnasInventario", columnasInventario);
		}

		internal static void AlmacenarNombreDeHoja(string linkHojaConsulta, string nombreHoja)
		{
			GuardarValorEnCuentaLocal(linkHojaConsulta + "|nombre", nombreHoja);
		}

		internal static void AlmacenarLinkHistoricosDeHoja(string linkHojaConsulta, string linkHojaHistoricos)
		{
			GuardarValorEnCuentaLocal(linkHojaConsulta + "|historico", linkHojaHistoricos);
		}

		internal static void AlmacenarLinkHistoricosCeldasDeHoja(string linkHojaConsulta, string linkHojaHistoricosCeldas)
		{
			GuardarValorEnCuentaLocal(linkHojaConsulta + "|historicoCeldas", linkHojaHistoricosCeldas);
		}

		public static void AlmacenarLinkHojaPuntosVentaDeHoja(string linkHojaConsulta, string linkHojaPuntosVenta)
		{
			GuardarValorEnCuentaLocal(linkHojaConsulta + "|hojaPuntosVenta", linkHojaPuntosVenta);
		}

		internal static void AlmacenarNombreUsuarioGoogle(string nombreUsuarioGoogle)
		{
			GuardarValorEnCuentaLocal("nombreUsuarioGoogle", nombreUsuarioGoogle);
		}

		internal static void AlmacenarLinkHojaHistoricos(string linkHojaHistoricos)
		{
			GuardarValorEnCuentaLocal("linkHojaHistoricos", linkHojaHistoricos);
		}

		internal static void AlmacenarPuntosVenta(string puntosVenta)
		{
			GuardarValorEnCuentaLocal("puntosVenta", puntosVenta);
		}

		internal static void AlmacenarTokenDeBaseDeDatos(string tokenDeAcceso)
		{
			GuardarValorEnCuentaLocal("tokenDeBaseDeDatos", tokenDeAcceso);
		}

		internal static void AlmacenarUsuarioDeBaseDeDatos(string usuario)
		{
			GuardarValorEnCuentaLocal("usuarioDeBaseDeDatos", usuario);
		}

		public static void AlmacenarLinkHojaRelacionesInsumoProducto(string linkHoja, string linkHojaRelacionesInsumoProducto)
		{
			GuardarValorEnCuentaLocal(linkHoja + "|relacionesInsumoProducto", linkHojaRelacionesInsumoProducto);
		}

		public static void AlmacenarLinkHojaClientes(string linkHojaClientes)
		{
			GuardarValorEnCuentaLocal("linkHojaClientes", linkHojaClientes);
		}

		public static void AlmacenarLinkHojaPedidos(string linkHojaPedidos)
		{
			GuardarValorEnCuentaLocal("linkHojaPedidos", linkHojaPedidos);
		}

		internal static void AlmacenarRelacionesInsumoProducto(string relacionesInsumoProducto)
		{
			GuardarValorEnCuentaLocal("relacionesInsumoProducto", relacionesInsumoProducto);
		}

		internal static void AlmacenarColumnasProducto(string columnasProducto)
		{
			GuardarValorEnCuentaLocal("columnasProducto", columnasProducto);
		}

		internal static bool VerificarHojaUsada(string linkHojaConsulta)
		{
			return _cuenta != null && _cuenta.Properties.ContainsKey(linkHojaConsulta + "|nombre");
		}

		internal static bool VerificarHojaUsadaConColumnas(string linkHojaConsulta)
		{
			return _cuenta != null && _cuenta.Properties.ContainsKey(linkHojaConsulta + "|ver")
						 && _cuenta.Properties.ContainsKey(linkHojaConsulta + "|inventario") && _cuenta.Properties.ContainsKey(linkHojaConsulta + "|nombre");
		}

		internal static bool VerificarHojaUsadaPorNombre(string nombreHojaConsulta)
		{
			if (_cuenta == null) return false;
			foreach (var llaveValor in _cuenta.Properties)
			{
				if (llaveValor.Key.Contains("|nombre") && llaveValor.Value.Equals(nombreHojaConsulta))
					return true;
			}
			return false;
		}

		internal static bool VerificarHojaHistoricosUsada(string linkHoja)
		{
			if (_cuenta == null) return false;
			foreach (var llaveValorH in _cuenta.Properties)
			{
				if (llaveValorH.Key.Contains("|historico") && llaveValorH.Value.Equals(linkHoja))
					return true;
			}
			return false;
		}

		internal static bool VerificarHojaPuntosVentaUsada(string linkHoja)
		{
			if (_cuenta == null) return false;
			foreach (var llaveValorH in _cuenta.Properties)
			{
				if (llaveValorH.Key.Contains("|hojaPuntosVenta") && llaveValorH.Value.Equals(linkHoja))
					return true;
			}
			return false;
		}

		internal static bool VerificarHojaUsadaRecuperarColumnas(string linkHojaConsulta)
		{
			//Si no hay columnas para esta hoja deducimos que la hoja no se ha usado, si hay, las cargamos.
			if (!VerificarHojaUsadaConColumnas(linkHojaConsulta))
				return false;

			AlmacenarColumnasParaVer(RecuperarValorDeCuentaLocal(linkHojaConsulta + "|ver"));
			AlmacenarColumnasInventario(RecuperarValorDeCuentaLocal(linkHojaConsulta + "|inventario"));
			return true;
		}

		internal static string ObtenerTokenActualDeGoogle()
		{
			return RecuperarValorDeCuentaLocal("tokenDeGoogle");
		}

		internal static DateTime? ObtenerFechaExpiracionToken()
		{
			var valor = RecuperarValorDeCuentaLocal("fechaExpiracion");
			return !string.IsNullOrEmpty(valor) ? Convert.ToDateTime(valor) : (DateTime?)null;
		}

		internal static string ObtenerLinkHojaConsulta()
		{
			return RecuperarValorDeCuentaLocal("linkHojaConsulta");
		}

		internal static string ObtenerColumnasParaVer()
		{
			return RecuperarValorDeCuentaLocal("columnasParaVer");
		}

		internal static string ObtenerColumnasInventario()
		{
			return RecuperarValorDeCuentaLocal("columnasInventario");
		}

		internal static string ObtenerNombreUsuarioGoogle()
		{
			return RecuperarValorDeCuentaLocal("nombreUsuarioGoogle");
		}

		internal static string ObtenerLinkHojaHistoricos()
		{
			return RecuperarValorDeCuentaLocal("linkHojaHistoricos");
		}

		internal static string ObtenerLinkHojaHistoricosCeldas(string link)
		{
			return RecuperarValorDeCuentaLocal(link + "|historicoCeldas");
		}

		internal static string ObtenerLinkHojaHistoricosParaLinkHoja(string link)
		{
			return RecuperarValorDeCuentaLocal(link + "|historico");
		}

		internal static string ObtenerLinkHojaClientes()
		{
			return RecuperarValorDeCuentaLocal("linkHojaClientes");
		}

		internal static string ObtenerLinkHojaPedidos()
		{
			return RecuperarValorDeCuentaLocal("linkHojaPedidos");
		}

		internal static string ObtenerPuntosVenta()
		{
			return RecuperarValorDeCuentaLocal("puntosVenta");
		}

		internal static string ObtenerRelacionesInsumoProducto()
		{
			return RecuperarValorDeCuentaLocal("relacionesInsumoProducto");
		}

		internal static string ObtenerTokenActualDeBaseDeDatos()
		{
			return RecuperarValorDeCuentaLocal("tokenDeBaseDeDatos");
		}

		internal static string ObtenerUsuarioDeBaseDeDatos()
		{
			return RecuperarValorDeCuentaLocal("usuarioDeBaseDeDatos");
		}

		internal static string ObtenerAccesoDatos()
		{
			return RecuperarValorDeCuentaLocal("accesoDatos");
		}

		internal static string ObtenerNombreHoja(string linkHojaConsulta)
		{
			return RecuperarValorDeCuentaLocal(linkHojaConsulta + "|nombre");
		}

		internal static List<string> ObtenerTodosLosNombresDeHojas()
		{
			var nombres = new List<string>();

			RecuperarCuentaLocal();
			if (_cuenta != null)
			{
				foreach (var llaveValor in _cuenta.Properties)
				{
					if (llaveValor.Key.Contains("|nombre"))
						nombres.Add(llaveValor.Value);
				}
			}

			return nombres;
		}

		internal static string ObtenerLinkHojaPorNombre(string nombreHoja)
		{
			var link = "";
			RecuperarCuentaLocal();

			if (_cuenta == null) return link;

			foreach (var llaveValor in _cuenta.Properties)
			{
				if (llaveValor.Key.Contains("|nombre") && llaveValor.Value.Equals(nombreHoja))
				{
					link = llaveValor.Key.Split('|')[0];
					break;
				}
			}

			return link;
		}

		internal static string ObtenerColumnasProductos()
        {
			return RecuperarValorDeCuentaLocal("columnasProducto");

		}

		internal static string CambiarHojaSeleccionada(string nombreHoja)
		{
			var link = ObtenerLinkHojaPorNombre(nombreHoja);
			if (string.IsNullOrEmpty(link)) return link;

			AlmacenarLinkHojaConsulta(link);
			AlmacenarColumnasParaVer(RecuperarValorDeCuentaLocal(link + "|ver"));
			AlmacenarColumnasInventario(RecuperarValorDeCuentaLocal(link + "|inventario"));
			AlmacenarLinkHojaHistoricos(RecuperarValorDeCuentaLocal(link + "|historico"));

			return link;
		}

		internal static bool ValidarTokenDeGoogle()
		{
			if (string.IsNullOrEmpty(ObtenerTokenActualDeGoogle()) || ObtenerFechaExpiracionToken() <= DateTime.Now)
				return false;
			return true;
		}

		internal static void ReiniciarHoja(string linkHoja)
		{
			RemoverValorEnCuentaLocal(linkHoja + "|nombre");
			RemoverValorEnCuentaLocal(linkHoja + "|historico");
			RemoverValorEnCuentaLocal(linkHoja + "|hojaPuntosVenta");
			RemoverValorEnCuentaLocal(linkHoja + "|ver");
			RemoverValorEnCuentaLocal(linkHoja + "|inventario");
			RemoverValorEnCuentaLocal(linkHoja + "|historicoCeldas");
			RemoverValorEnCuentaLocal(linkHoja + "|relacionesInsumoProducto");

			//Si es la hoja actual vuela la memoria de uso
			if (ObtenerLinkHojaConsulta() != linkHoja) return;
			RemoverValorEnCuentaLocal("linkHojaConsulta");
			RemoverValorEnCuentaLocal("linkHojaHistoricos");
			RemoverValorEnCuentaLocal("puntosVenta");
			RemoverValorEnCuentaLocal("columnasParaVer");
			RemoverValorEnCuentaLocal("columnasInventario");
			RemoverValorEnCuentaLocal("relacionesInsumoProducto");
		}

	}
}
