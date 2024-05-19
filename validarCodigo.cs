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
//cifrado contrasena
using System.Security.Cryptography;
using System.Text;

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
            string contrasena = req.Query["contrasena"];
            if(ObtenerValor(correo)==int.Parse(codigo)){
                eliminarValor(correo);
                setContrasena(correo, contrasena);
                return new OkObjectResult(new { message = "Se restableció la contraseña correctamente." });
            }else
                return new BadRequestObjectResult(JsonConvert.SerializeObject("Código incorrecto"));

        }

        public static int ObtenerValor(string correo){
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
                
                cmd.CommandText = "select validador from especialista where correo=@correo";
                cmd.Parameters.AddWithValue("@correo", correo);
                resultado=cmd.ExecuteScalar();
                if(resultado!=null&& int.TryParse(resultado.ToString(), out valor))
                    return valor;
                cmd.CommandText = "select validador from paciente where correo=@correo1";
                cmd.Parameters.AddWithValue("@correo1", correo);
                resultado=cmd.ExecuteScalar();
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

        public static void eliminarValor(string correo){
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
                
                cmd.CommandText = "update especialista set validador=null where correo=@correo";
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "update paciente set validador=null where correo=@correo1";
                cmd.Parameters.AddWithValue("@correo1", correo);
                cmd.ExecuteNonQuery();

            }catch (Exception e){
                throw new Exception(e.Message);
            }finally{
                conexion.Close();
            }
        }

        public static void setContrasena(string correo, string contrasena){
            string Server = Environment.GetEnvironmentVariable("Server");
            string UserID = Environment.GetEnvironmentVariable("UserID");
            string Password = Environment.GetEnvironmentVariable("Password");
            string DB = Environment.GetEnvironmentVariable("DataBase");
            //Crear cadena de conexion
            string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";Database=" + DB + ";SslMode=Preferred;";
            string encodeContrasena=encode(contrasena);
            var conexion = new MySqlConnection(conDB);
            //abrir conexion
            conexion.Open();
            try{
                var cmd= new MySqlCommand();
                cmd.Connection = conexion;
                
                cmd.CommandText = "update especialista set contrasena=@contrasena where correo=@correo";
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@contrasena", encodeContrasena);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "update paciente set contrasena=@contrasena1 where correo=@correo1";
                cmd.Parameters.AddWithValue("@correo1", correo);
                cmd.Parameters.AddWithValue("@contrasena1", encodeContrasena);
                cmd.ExecuteNonQuery();

            }catch (Exception e){
                throw new Exception(e.Message);
            }finally{
                conexion.Close();
            }
        }

        static string encode(string pass){
                string salt = Environment.GetEnvironmentVariable("salt");
                byte[] combinedBytes = Encoding.UTF8.GetBytes(pass + salt);

                // Calcular el hash con SHA-256
                byte[] hashBytes;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hashBytes = sha256.ComputeHash(combinedBytes);
                }

                // Convertir el hash a una cadena hexadecimal
                string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hash;
            }

    }
}
