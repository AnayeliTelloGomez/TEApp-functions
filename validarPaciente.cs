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

namespace TEAPP
{
    public static class validarPaciente
    {
        [FunctionName("validarPaciente")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
           log.LogInformation("C# HTTP trigger function processed a request.");

            string correo = req.Query["correo"];
            string idEsp = req.Query["idEsp"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            correo = correo ?? data?.correo;
            idEsp = idEsp ?? data?.idEsp;

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

            try{
                //Crear la variable tipo comando en MySQL y asignarle sus caracteristicas
                var cmdAltaPaciente = new MySqlCommand();
                cmdAltaPaciente.Connection = conexion;
                cmdAltaPaciente.CommandText = "UPDATE paciente SET idespecialista=(SELECT idespecialista FROM Especialista WHERE correo=@idEsp) where correo=@correo";
                cmdAltaPaciente.Parameters.AddWithValue("@correo", correo);
                cmdAltaPaciente.Parameters.AddWithValue("@idEsp",idEsp);

                //executar Query (non porque no devuelve datos)
                cmdAltaPaciente.ExecuteNonQuery();

                return new OkObjectResult(new { message = "Usuario validado correctamente." });

            }catch (Exception e){
                //throw new Exception(e.Message);
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }finally{
                conexion.Close();
            }
        }
    }
}
