using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace TEAPP
{
    public static class estadisticaPaciente
    {
        class actividad{
            public string idpaciente;
            public string act;
            public string emocion;
            public string reactivos;
        }

        class estadistica{
            public string idactividad;
            public string correctas;
            public string incorrectas;
            public string tiempo; 
            
        }

        [FunctionName("estadisticaPaciente")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try{
            log.LogInformation("Alta usuario.");
            //leer la solicitud HTTP y almacenar en una cadena
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //pasar la cadena leída en la solicitud HTTP a un objeto ripo paramAltaPaciente
            log.LogInformation("reqbody", requestBody);
            actividad data = JsonConvert.DeserializeObject<actividad>(requestBody);

            

            //paramatremos para conectar con la base
            string Server = Environment.GetEnvironmentVariable("Server");
            string UserID = Environment.GetEnvironmentVariable("UserID");
            string Password = Environment.GetEnvironmentVariable("Password");
            string DB = Environment.GetEnvironmentVariable("DataBase");
            //Crear cadena de conexion
            string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";Database=" + DB + ";SslMode=Preferred;";
            List<estadistica> estadisticas=new List<estadistica>();
            
            MySqlConnection conexion= new MySqlConnection(conDB);
            try{
                    using(conexion){
                        conexion.Open();
                        string query = "select idactividad,correctas,incorrectas, tiempo from actividad_asignada where paciente=@idpac and actividad=@act and emocion=@emocion and numreactivos=@reactivos and realizada=1;";
                        MySqlCommand cmdActividad= new MySqlCommand(query,conexion);
                        cmdActividad.Parameters.AddWithValue("@idpac",int.Parse(data.idpaciente));
                        cmdActividad.Parameters.AddWithValue("@act",data.act);
                        cmdActividad.Parameters.AddWithValue("@emocion",data.emocion);
                        cmdActividad.Parameters.AddWithValue("@reactivos",int.Parse(data.reactivos));

                        using (MySqlDataReader reader= cmdActividad.ExecuteReader()){
                            while (reader.Read()){
                                var estadistica = new estadistica{
                                    idactividad = Convert.ToInt32(reader["idactividad"]).ToString(),
                                    correctas = Convert.ToInt32(reader["correctas"]).ToString(),
                                    incorrectas = Convert.ToInt32(reader["incorrectas"]).ToString(),
                                    tiempo = Convert.ToDouble(reader["tiempo"]).ToString()
                                };
                                estadisticas.Add(estadistica);
                            }
                        }       
                    }
                    string jsonPacientes = JsonConvert.SerializeObject(estadisticas, Formatting.Indented);
                    //Console.WriteLine(jsonPacientes);
                    return new OkObjectResult(jsonPacientes);
                    
            }catch (Exception e){
                //throw new Exception(e.Message);
                return new BadRequestObjectResult(e.Message);
            }
            finally{
                conexion.Close();
            }
            }catch (Exception e){
                Console.WriteLine(e.Message);
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}
