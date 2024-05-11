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
using System.Data;


namespace TEAPP
{

    public static class despliegueEspecialista
    {


        class PacienteConsulta{
            public string correo;
            public string nombres;
            public string paterno;
            public string materno;
            public int idespecialista;
        }

        [FunctionName("despliegueEspecialista")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
             try
            {
            //paramatremos para conectar con la base
            string Server = Environment.GetEnvironmentVariable("Server");
            string UserID = Environment.GetEnvironmentVariable("UserID");
            string Password = Environment.GetEnvironmentVariable("Password");
            string DB = Environment.GetEnvironmentVariable("DataBase");
            //Crear cadena de conexion
            string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";Database=" + DB + ";SslMode=Preferred;";
            List<PacienteConsulta> pacientes=new List<PacienteConsulta>();
            //crear la conexion
            //var conexion = new MySqlConnection(conDB);
            //abrir conexion
            //conexion.Open();
            MySqlConnection conexion= new MySqlConnection(conDB);
            try{
                    using(conexion){
                        conexion.Open();
                        string query = "select correo, nombres, paterno, materno, idadministrador from especialista order by idadministrador ASC;";
                        MySqlCommand cmdPacientes= new MySqlCommand(query,conexion);

                        using (MySqlDataReader reader= cmdPacientes.ExecuteReader()){
                            while (reader.Read()){
                                var paciente = new PacienteConsulta{
                                    correo=reader["correo"].ToString(),
                                    nombres=reader["nombres"].ToString(),
                                    paterno=reader["paterno"].ToString(),
                                    materno=reader["materno"].ToString(),
                                    idespecialista=reader.GetInt32(reader.GetOrdinal("idadministrador"))
                                };
                                pacientes.Add(paciente);
                            }
                        }       
                    }
                    string jsonPacientes = JsonConvert.SerializeObject(pacientes, Formatting.Indented);
                    //Console.WriteLine(jsonPacientes);
                    return new OkObjectResult(jsonPacientes);
                    
            }catch (Exception e){
                throw new Exception(e.Message);
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
