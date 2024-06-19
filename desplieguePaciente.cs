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
    public static class desplieguePaciente
    {
        /*Esta clase se utiliza para serializar el objeto
         y mandarlo como json si la función se realizó correctamente */
        class PacienteConsulta{
            public string correo;
            public string nombres;
            public string paterno;
            public string materno;
            public int idespecialista;
        }
        //nombre con el cual se llama a la AZ Function
        [FunctionName("desplieguePaciente")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            //paramatremos para conectar con la base
            string Server = Environment.GetEnvironmentVariable("Server");
            string UserID = Environment.GetEnvironmentVariable("UserID");
            string Password = Environment.GetEnvironmentVariable("Password");
            string DB = Environment.GetEnvironmentVariable("DataBase");
            //Crear cadena de conexion
            string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + 
                            ";Database=" + DB + ";SslMode=Preferred;";
            List<PacienteConsulta> pacientes=new List<PacienteConsulta>();
            //crear la conexion
            MySqlConnection conexion= new MySqlConnection(conDB);
            try{
                    using(conexion){
                        conexion.Open();
                        //Comando para consulta
                        string query = "select correo, nombres, paterno, materno, idespecialista"+
                                        "from paciente order by idespecialista ASC;";
                        //ejecución de comando
                        MySqlCommand cmdPacientes= new MySqlCommand(query,conexion);
                        
                        /*se lee la consulta y se crea un objeto tipo paciente que
                         posteriormente se agrega auna lista de pacientes*/
                        using (MySqlDataReader reader= cmdPacientes.ExecuteReader()){
                            while (reader.Read()){
                                var paciente = new PacienteConsulta{
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
                    /*se convierte la lista de pacientes a un json para enviarlo como resultado de la función*/
                    string jsonPacientes = JsonConvert.SerializeObject(pacientes, Formatting.Indented);
                    return new OkObjectResult(jsonPacientes);
                    
            }catch (Exception e){
                throw new Exception(e.Message);
            }
            finally{
                conexion.Close();
            }
        }
    }
}
