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
//creacion token
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace TEAPP
{
    public static class inicioSesion
    {
        class UsuarioInicio{
            public string correo;
            public string contrasena;
            public string tipo;
        }

        class ParamInicio{
            public UsuarioInicio usuarioInicio;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [FunctionName("inicioSesion")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            try{
                log.LogInformation("Inicio de sesión.");
                //leer la solicitud http y almacenarlo en una cadena
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamInicio data = JsonConvert.DeserializeObject<ParamInicio>(requestBody);
                UsuarioInicio usuario = data.usuarioInicio;

                if (usuario.correo.Equals("") || usuario.correo == null) throw new Exception("Ingresar correo electrónico.");
                if (usuario.contrasena.Equals("") || usuario.contrasena == null) throw new Exception("Ingresar contraseña.");
                
                string hashedPassword = encode(usuario.contrasena);

                //paramatremos para conectar con la base
                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string DB = Environment.GetEnvironmentVariable("DataBase");
                //Crear cadena de conexion
                string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";Database=" + DB + ";SslMode=Preferred;";
                //crear la conexion
                var conexion = new MySqlConnection(conDB);
                //abrir conexion
                conexion.Open();

                MySqlTransaction transaccion = conexion.BeginTransaction();

                try
                {
                    var cmdInicio = new MySqlCommand();
                    cmdInicio.Connection = conexion;
                    cmdInicio.Transaction = transaccion;
                    object resultado = new object();
                    if (usuario.tipo.Equals("2")){
                        cmdInicio.CommandText = "select * from paciente where correo=@correo and contrasena=@contrasena";
                        cmdInicio.Parameters.AddWithValue("@correo", usuario.correo);
                        cmdInicio.Parameters.AddWithValue("@contrasena", hashedPassword);
                        resultado = cmdInicio.ExecuteScalar();
                    }
                    else{
                        cmdInicio.CommandText = "select * from especialista where correo=@correo and contrasena=@contrasena";
                        cmdInicio.Parameters.AddWithValue("@correo", usuario.correo);
                        cmdInicio.Parameters.AddWithValue("@contrasena", hashedPassword);
                        resultado = cmdInicio.ExecuteScalar();
                    }
                    if (resultado != null && Convert.ToInt32(resultado) > 0){
                        transaccion.Commit();
                        //creacion del token
                        
                        /*var claims = new[]{
                            new Claim(ClaimTypes.Email, usuario.correo),
                            new Claim(ClaimTypes.Role, usuario.tipo)
                        };

                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("salt")));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        var token = new JwtSecurityToken(
                            claims: claims,
                            expires: DateTime.Now.AddMinutes(30),
                            signingCredentials: creds
                        );

                        return new JwtSecurityTokenHandler().WriteToken(token);*/

                        return new OkObjectResult("Credenciales correctas");
                    }else{
                        transaccion.Commit();
                        return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("Credenciales erroneas. correo"+usuario.correo+usuario.contrasena)));
                    }
                }catch (Exception e){
                    transaccion.Rollback();
                    throw new Exception(e.Message);
                }
                finally{
                    conexion.Close();
                }
            }catch (Exception e){
                Console.WriteLine(e.Message);
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
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
}
