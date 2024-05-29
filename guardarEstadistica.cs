using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using MySql.Data.MySqlClient;
using Google.Protobuf.Reflection;

namespace TEAPP
{
    public static class guardarEstadistica
    {
        class estadistica{
            public string idact;
            public string correctas;
            public string incorrectas;
            public string tiempo; 
            
        }

        class Error{
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }


        [FunctionName("guardarEstadistica")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("Alta usuario.");
                //leer la solicitud HTTP y almacenar en una cadena
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                //pasar la cadena leída en la solicitud HTTP a un objeto ripo paramAltaPaciente
                log.LogInformation("reqbody", requestBody);
                estadistica data = JsonConvert.DeserializeObject<estadistica>(requestBody);

                //leer los parametros para establecer conexión
                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string DB = Environment.GetEnvironmentVariable("DataBase");

                //Crear string de conexion a BD
                string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + DB + ";SslMode=Preferred;";
                //crear conex[on]
                var conexion = new MySqlConnection(conDB);
                //abrir conexion
                conexion.Open();

                //iniciar transaccion para asegurar integridad de datos
                MySqlTransaction transaccion = conexion.BeginTransaction();

                try
                {
                    //Crear la variable tipo comando en MySQL y asignarle sus caracteristicas
                    var cmdGuardarEstadisticas = new MySqlCommand();
                    cmdGuardarEstadisticas.Connection = conexion;
                    cmdGuardarEstadisticas.Transaction = transaccion;
                    cmdGuardarEstadisticas.CommandText = "UPDATE actividad_asignada SET correctas=@correctas, incorrectas=@incorrectas , tiempo=@tiempo, realizada = 1  WHERE idactividad = @idact";
                    cmdGuardarEstadisticas.Parameters.AddWithValue("@idact",int.Parse(data.idact));
                    cmdGuardarEstadisticas.Parameters.AddWithValue("@correctas", int.Parse(data.correctas));
                    cmdGuardarEstadisticas.Parameters.AddWithValue("@incorrectas", int.Parse(data.incorrectas));
                    float tiempo=float.Parse(data.tiempo);
                    cmdGuardarEstadisticas.Parameters.AddWithValue("@tiempo", tiempo);
                    

                    //executar Query (non porque no devuelve datos)
                    cmdGuardarEstadisticas.ExecuteNonQuery();

                    transaccion.Commit();
                    return new OkObjectResult(new {message="Estadistica registrada correctamente."});

                }
                catch (Exception e)
                {
                    transaccion.Rollback();
                    throw new Exception(e.Message);
                }
                finally
                {
                    conexion.Close();
                }
                //return new OkObjectResult(new {message="hola"+float.Parse(data.tiempo)});
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }

        }
    }
}
