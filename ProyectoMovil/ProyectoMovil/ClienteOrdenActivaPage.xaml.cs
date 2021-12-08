using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProyectoMovil.Controller;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ProyectoMovil
{
    public partial class ClienteOrdenActivaPage : ContentPage
    {
        String usuario;
        String id;
        String ordenID = null;
        public ClienteOrdenActivaPage()
        {
            InitializeComponent();
            ObtenerCredenciales();
            ClienteListaOrdenes();
        }

        private async void ObtenerCredenciales()
        {
            usuario = await SecureStorage.GetAsync("usuario");
            id = await SecureStorage.GetAsync("ID");
        }

        private async void ClienteListaOrdenes()
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                using (HttpClient cliente = new HttpClient())
                {
                    object obj = new { usuario = usuario };

                    String jsonContent = JsonConvert.SerializeObject(obj);
                    StringContent contenido = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                    var response = await cliente.PutAsync(Configuraciones.EndPointOrden, contenido);

                    if (response.IsSuccessStatusCode)
                    {
                        String respuesta = response.Content.ReadAsStringAsync().Result;

                        if (respuesta.Length > 65)
                        {
                            dynamic array = JsonConvert.DeserializeObject(respuesta);

                            List<Modelos.Orden> lista = new List<Modelos.Orden>();

                            foreach (var item in array.orden)
                            {
                                if (!(item.estado.ToString() == "Entregado"))
                                {
                                    lista.Add(new Modelos.Orden(item.pk.ToString(), item.cliente.ToString(), item.productos.ToString(),
                                    item.empleado_orden.ToString(), item.subtotal.ToString(), item.isv.ToString(), item.Total.ToString(),
                                    item.t_pago.ToString(), item.fecha_registro.ToString(), item.estado.ToString()));
                                }
                            }
                            listaOrdenActiva.ItemsSource = lista;
                        }
                        else
                        {
                            listaOrdenActiva.ItemsSource = null;
                            await DisplayAlert("Notificación", "No hay orden activa. Haga un pedido", "OK");
                            return;
                        }
                    }
                }
            }
            else
            {
                await DisplayAlert("Error", "Verifique su conexión de Internet", "OK");
            }
        }

        private async void btnVerMapa_Clicked(System.Object sender, System.EventArgs e)
        {
            //var item = e. as MyFlyoutPageFlyoutMenuItem;


            object obj = new { ID = ordenID };

            Uri RequestUri = new Uri("https://appmovil2.herokuapp.com/Api/Pedido");

            var client = new HttpClient();
            var json = JsonConvert.SerializeObject(obj);
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Put,
                RequestUri = RequestUri
            };
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
               
                String jsonx = response.Content.ReadAsStringAsync().Result;
                JObject jsons = JObject.Parse(jsonx);
                String Mensaje = jsons["Mensaje"].ToString();
                await DisplayAlert("Alerta", ""+jsons, "OK");
                string ID = jsons["Pedidos"][0]["NumeroPedido"].ToString();
                string nombre = jsons["Pedidos"][0]["NombreCliente"].ToString();
                string lonempleado = jsons["Pedidos"][0]["lonEmpleado"].ToString();
                string latempleado = jsons["Pedidos"][0]["latEmpleado"].ToString();
                string estado = jsons["Pedidos"][0]["estadoOrden"].ToString();

                if (estado == "Activo")
                {
                    await DisplayAlert("Alerta", "Sigue en espera", "OK");
                }
                else if (estado == "Proceso")
                {
                    await DisplayAlert("Alerta", "Esta en camino", "OK");
                    if (!double.TryParse(latempleado, out double lat))
                        return;
                    if (!double.TryParse(lonempleado, out double lng))
                        return;

                    await Map.OpenAsync(lat, lng, new MapLaunchOptions
                    {
                        Name = nombre,
                        NavigationMode = NavigationMode.Driving
                    });
                }
                else
                {

                }
            }
            else
            {
                await DisplayAlert("Alerta", "Ha ocurrido un error", "OK");
            }
        }

        void listaOrdenActiva_SelectionChanged(System.Object sender, Xamarin.Forms.SelectionChangedEventArgs e)
        {
            UpdateSelectionData(e.PreviousSelection, e.CurrentSelection);
        }

        private void UpdateSelectionData(IEnumerable<object> previousSelectedContact, IEnumerable<object> currentSelectedContact)
        {
            var item = currentSelectedContact.FirstOrDefault() as Modelos.Orden;
            ordenID = item.id;
        }
    }
}
