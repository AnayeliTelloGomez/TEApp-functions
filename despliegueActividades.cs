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
    public static class despliegueActividades
    {
        class actividad{
            public string act;
            public string emocion;
            public string reactivos;
        }
        [FunctionName("despliegueActividades")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try{
            log.LogInformation("C# HTTP trigger function processed a request.");

            string correo = req.Query["correo"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            correo = correo ?? data?.correo;

            

            //paramatremos para conectar con la base
            string Server = Environment.GetEnvironmentVariable("Server");
            string UserID = Environment.GetEnvironmentVariable("UserID");
            string Password = Environment.GetEnvironmentVariable("Password");
            string DB = Environment.GetEnvironmentVariable("DataBase");
            //Crear cadena de conexion
            string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";Database=" + DB + ";SslMode=Preferred;";
            List<actividad> actividades=new List<actividad>();
            
            MySqlConnection conexion= new MySqlConnection(conDB);
            try{
                    using(conexion){
                        conexion.Open();
                        string query = "select actividad, emocion, numreactivos from actividad_asignada";
                        MySqlCommand cmdPacientes= new MySqlCommand(query,conexion);
                        cmdPacientes.Attributes.SetAttribute("@correo", correo);

                        using (MySqlDataReader reader= cmdPacientes.ExecuteReader()){
                            while (reader.Read()){
                                var actividad = new actividad{
                                    act=reader["actividad"].ToString(),
                                    emocion=reader["emocion"].ToString(),
                                    reactivos=reader["numreactivos"].ToString(),
                                    
                                };
                                actividades.Add(actividad);
                            }
                        }       
                    }
                    string jsonPacientes = JsonConvert.SerializeObject(actividades, Formatting.Indented);
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
