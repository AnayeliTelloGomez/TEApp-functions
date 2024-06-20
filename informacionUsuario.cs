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
using System.Collections.Generic;

namespace TEApp
{
    public static class informacionUsuario
    {
        class PacienteConsulta{
            public string correo;
            public string nombres;
            public string paterno;
            public string materno;
        }
        class Error{
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }
        
        [FunctionName("informacionUsuario")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try{
                log.LogInformation("info usuario.");
                string correo = req.Query["correo"];
                string tipo = req.Query["tipo"];
                //paramatremos para conectar con la base
                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string DB = Environment.GetEnvironmentVariable("DataBase");
                //Crear cadena de conexion
                string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";Database=" + DB + ";SslMode=Preferred;";
                List<PacienteConsulta> pacientes=new List<PacienteConsulta>();
                var paciente=new PacienteConsulta();
                //crear la conexion
                //var conexion = new MySqlConnection(conDB);
                //abrir conexion
                //conexion.Open();
                MySqlConnection conexion= new MySqlConnection(conDB);
                try{
                        using(conexion){
                            conexion.Open();
                            string query="";
                            if(tipo.Equals("2")){
                                query = "select correo, nombres, paterno, materno from paciente where correo=@correo";
                            }else if(tipo.Equals("1")){
                                query = "select correo, nombres, paterno, materno from especialista where correo=@correo";
                            }else{
                                query = "select correo, nombres, paterno, materno from administrador where correo=@correo";
                            }
                            MySqlCommand cmdPacientes= new MySqlCommand(query,conexion);

                            cmdPacientes.Parameters.AddWithValue("@correo", correo);

                            using (MySqlDataReader reader= cmdPacientes.ExecuteReader()){
                                while (reader.Read()){
                                    paciente = new PacienteConsulta{
                                        correo=reader["correo"].ToString(),
                                        nombres=reader["nombres"].ToString(),
                                        paterno=reader["paterno"].ToString(),
                                        materno=reader["materno"].ToString(),
                                    };
                                    pacientes.Add(paciente);
                                }
                            }       
                        }
                        string jsonPacientes = JsonConvert.SerializeObject(paciente, Formatting.Indented);
                        //Console.WriteLine(jsonPacientes);
                        return new OkObjectResult(jsonPacientes);
                        
                }catch (Exception e){
                    throw new Exception(e.Message);
                }
                finally{
                    conexion.Close();
                }

            }catch(Exception e){
                Console.WriteLine(e.Message);
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}
