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
    public static class validarCodigo
    {
        //paramatremos para conectar con la base
        [FunctionName("validarCodigo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string correo = req.Query["correo"];
            string codigo = req.Query["codigo"];
            string tipo = req.Query["tipo"];
            if(ObtenerValor(tipo, correo)==int.Parse(codigo))
                return new OkObjectResult(new { message = "Validación correcta." });
            else
                return new BadRequestObjectResult(JsonConvert.SerializeObject("Código incorrecto"));

        }

        public static int ObtenerValor(string tipo, string correo){
            string Server = Environment.GetEnvironmentVariable("Server");
            string UserID = Environment.GetEnvironmentVariable("UserID");
            string Password = Environment.GetEnvironmentVariable("Password");
            string DB = Environment.GetEnvironmentVariable("DataBase");
            //Crear cadena de conexion
            string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";Database=" + DB + ";SslMode=Preferred;";
            
            int valor = 0;

            var conexion = new MySqlConnection(conDB);
            //abrir conexion
            conexion.Open();
            try{
                var cmd= new MySqlCommand();
                cmd.Connection = conexion;
                object resultado=new object();
                if(tipo.Equals("1")){
                    cmd.CommandText = "select validador from especialista where correo=@correo";
                    cmd.Parameters.AddWithValue("@correo", correo);
                    resultado=cmd.ExecuteScalar();
                }else{
                    cmd.CommandText = "select validador from paciente where correo=@correo";
                    cmd.Parameters.AddWithValue("@correo", correo);
                    resultado=cmd.ExecuteScalar();
                }
                if(resultado!=null&& int.TryParse(resultado.ToString(), out valor))
                    return valor;
                else
                    return 0;
            }catch (Exception e){
                throw new Exception(e.Message);
            }finally{
                conexion.Close();
            }
        }

    }
}
