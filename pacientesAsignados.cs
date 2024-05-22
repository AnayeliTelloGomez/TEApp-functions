using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Data;

namespace TEAPP
{
    public static class pacientesAsignados
    {
        class PacienteConsulta{
            public int idpaciente;
            public string correo;
            public string nombres;
            public string paterno;
            public string materno;
            public int idespecialista;
        }


        [FunctionName("pacientesAsignados")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try{
            log.LogInformation("C# HTTP trigger function processed a request.");

            string idEsp = req.Query["idEsp"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            idEsp = idEsp ?? data?.idEsp;

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
                        string query = "select idpaciente,correo, nombres, paterno, materno, idespecialista from paciente where idespecialista = (SELECT idespecialista FROM Especialista WHERE correo=@idEsp);";
                        MySqlCommand cmdPacientes= new MySqlCommand(query,conexion);
                        cmdPacientes.Parameters.AddWithValue("@idEsp", idEsp);
                        using (MySqlDataReader reader= cmdPacientes.ExecuteReader()){
                            while (reader.Read()){
                                var paciente = new PacienteConsulta{
                                    idepaciente=reader.GetInt32(reader.GetOrdinal("idpaciente")),
                                    correo=reader["correo"].ToString(),
                                    nombres=reader["nombres"].ToString(),
                                    paterno=reader["paterno"].ToString(),
                                    materno=reader["materno"].ToString(),
                                    idespecialista=reader.GetInt32(reader.GetOrdinal("idespecialista"))
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
