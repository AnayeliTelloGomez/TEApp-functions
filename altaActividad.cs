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
    public static class altaActividad
    {
        class actividad{
            public string idpaciente;
            public string idespecialista;
            public string act;
            public string emocion;
            public string reactivos; 
            

        }

        class Error{
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [FunctionName("altaActividad")]
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
                actividad data = JsonConvert.DeserializeObject<actividad>(requestBody);

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
                    var cmdAltaPaciente = new MySqlCommand();
                    cmdAltaPaciente.Connection = conexion;
                    cmdAltaPaciente.Transaction = transaccion;
                    cmdAltaPaciente.CommandText = "insert into actividad_asignada (idactividad,paciente,especialista,actividad,emocion,numreactivos) values (0,@idpaciente,@idespecialista,@emocion,@act,@reactivos)";
                    cmdAltaPaciente.Parameters.AddWithValue("@idpaciente",int.Parse(data.idpaciente));
                    cmdAltaPaciente.Parameters.AddWithValue("@idespecialista", int.Parse(data.idespecialista));
                    cmdAltaPaciente.Parameters.AddWithValue("@emocion", data.emocion);
                    cmdAltaPaciente.Parameters.AddWithValue("@act", data.act);
                    cmdAltaPaciente.Parameters.AddWithValue("@reactivos", int.Parse(data.reactivos));
                    

                    //executar Query (non porque no devuelve datos)
                    cmdAltaPaciente.ExecuteNonQuery();

                    transaccion.Commit();
                    return new OkObjectResult(new {message="Actividad registrada correctamente."});

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
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}
