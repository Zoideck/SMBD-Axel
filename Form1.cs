﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace Proyecto_Archivos
{
    public partial class Form1 : Form
    {

        FileStream archivo; // Variable para el stream del archivo que estamos utilizando
        string RutaArchivo; // Variable que guarda la ruta del Diccionario de Datos
        string RutaArchivoDatos; // Variable que guarda la ruta del Archivo de Datos dependiendo la tabla 
        string RutaArchivoIndicePrimario; // Variable que guarda la ruta del Archivo de Indice Primario
        string RutaArchivoIndiceSecundario; // Variable que guarda la ruta del Archivo de Indice Secundario
        string RutaArchivoArbolPrimario; // Variable que guarda la ruta del Archivo de Arbol Primario
        string RutaArchivoArbolSecundario; // Variable que guarda la ruta del Archivo de Arbol Secundario
        string RutaCarpeta = ""; // Variable que guarda la ruta de la carpeta donde se guardan todos los archivos de determinada DB
        string RutaRelaciones = "";
        Archivo ManejoArchivo;
        ManejoArbol ManejadorArboles;
        Diccionario D;
        int indiceEntidadRegistro = -1;
        int indiceRegistroSeleccionado = -1;
        int IndexPrimario = -1;
        int indiceBloqueSecundarioSeleccionado = -1;
        int IndexSecundario = -1;
        int IndexArbolPrimario = -1;
        int IndexArbolSecundario = -1;
        long Cabecera = -1;
        bool busqueda;
        string AuxBusqueda;
        Arbol ArbolPrimario;
        Form2 FormAtributoFK;

        public Form1()
        {
            InitializeComponent();
            ManejoArchivo = new Archivo();
            D = new Diccionario();
            ManejadorArboles = new ManejoArbol();
            DGV_AtributosRegistro.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            ComboB_AccionesEntidad.SelectedIndex = 0;
            LB_Mod_NombreActual.Visible = false;
            LB_Mod_NuevoNombre.Visible = false;
            TextB_NombreActualEnt.Visible = false;
            TextB_NuevoNombEnt.Visible = false;
        }

        private void ComboB_AccionesEntidad_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboB_AccionesEntidad.SelectedIndex == 2)
            {
                LB_Mod_NombreActual.Visible = true;
                LB_Mod_NuevoNombre.Visible = true;
                TextB_NombreActualEnt.Visible = true;
                TextB_NuevoNombEnt.Visible = true;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {

            switch (ComboB_AccionesEntidad.SelectedIndex)
            {
                case 0: // Creamos una nueva entidad
                    if (string.IsNullOrEmpty(TextB_NombreEntidad.Text))
                    {
                        MessageBox.Show("Ingrese el nombre de la entidad");
                    }
                    else
                    {
                        AgregaNuevaEntidad(TextB_NombreEntidad.Text);
                    }
                    break;
                case 1: // Eliminamos la Entidad
                    if (File.Exists(RutaCarpeta + "\\" + TextB_NombreEntidad.Text + ".dat"))
                    {
                        MessageBox.Show("No se puede borrar la Entidad porque ya existen datos en la tabla");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(TextB_NombreEntidad.Text))
                        {
                            MessageBox.Show("Ingrese el nombre de la entidad");
                        }
                        else
                        {
                            if (D.Entidades.Count > 1)
                            {
                                EliminaEntidad(TextB_NombreEntidad.Text);
                            }
                            else
                            {
                                EliminaEntidad0(TextB_NombreEntidad.Text);
                            }
                        }
                    }
                    break;
                case 2: //Modificamos el nombre de la Entidad
                    if (string.IsNullOrEmpty(TextB_NombreActualEnt.Text) || string.IsNullOrEmpty(TextB_NuevoNombEnt.Text))
                    {
                        MessageBox.Show("Complete los campos necesarios por favor");
                    }
                    else
                    {
                        string NombreAnteriorEntidad = "";
                        string NuevoNombreEntidad = "";
                        foreach (Entidad en in D.Entidades)
                        {
                            if (en.ObtenCadenaNombre() == TextB_NombreActualEnt.Text)
                            {
                                NombreAnteriorEntidad = en.ObtenCadenaNombre();
                                en.Nombre_Entidad = ConvierteCadena(TextB_NuevoNombEnt.Text, 35);
                                ManejoArchivo.ModificaEntidad(en, RutaArchivo, archivo);
                                NuevoNombreEntidad = en.ObtenCadenaNombre();
                            }
                        }
                        if (BuscaRelacionTablaPadre(NombreAnteriorEntidad))
                        {
                            ActualizaRelacionTablaPadre(NombreAnteriorEntidad, NuevoNombreEntidad);
                        }
                        D.Entidades = D.Entidades.OrderBy(o => o.ObtenCadenaNombre()).ToList();
                        ManejoArchivo.ActualizaCabecera(RutaArchivo, archivo, D.Entidades[0].Direccion_Entidad);
                        Cabecera = ManejoArchivo.ObtenCabecera(RutaArchivo, archivo);
                        ManejoArchivo.GuardaRelaciones(D.Entidades, RutaRelaciones);
                        LB_Cabecera.Text = Cabecera.ToString();
                        ActEntidades();
                        ActualizaDGVEntidades();
                    }
                    TextB_NombreActualEnt.Clear();
                    TextB_NuevoNombEnt.Clear();
                    LB_Mod_NombreActual.Visible = false;
                    LB_Mod_NuevoNombre.Visible = false;
                    TextB_NombreActualEnt.Visible = false;
                    TextB_NuevoNombEnt.Visible = false;
                    break;
            }
            TextB_NombreEntidad.Clear();
        }

        public void AgregaNuevaEntidad(string NombreEntidad)
        {
            bool nuevaEntidad = true;
            long Direccion = ManejoArchivo.ObtenUltimaDireccion(RutaArchivo, archivo);
            char[] Auxiliar = ConvierteCadena(NombreEntidad, 35);
            byte[] Clave = ObtenClaveAleatoria();

            Entidad NuevaEntidad = new Entidad(Clave, Auxiliar, Direccion, -1, -1, -1);
            foreach (Entidad e in D.Entidades)
            {
                if (e.ObtenCadenaNombre() == NombreEntidad)
                {
                    MessageBox.Show("Ya existe una entidad con ese nombre, intente de nuevo");
                    nuevaEntidad = false;
                }
            }
            if (nuevaEntidad == true)
            {
                D.Entidades.Add(NuevaEntidad);
                ManejoArchivo.GuardaEntidad(NuevaEntidad, RutaArchivo, archivo);
                ReorganizaEntidades();
            }
            TextB_NombreAtributo.Clear();
        }

        public void EliminaEntidad(string NombreEntidad)
        {
            int aux = 0;
            if (!BuscaRelacionTablaPadre(NombreEntidad))
            {
                for (int i = 0; i < D.Entidades.Count; i++)
                {
                    if (D.Entidades[i].ObtenCadenaNombre() == NombreEntidad)
                    {
                        aux = i;
                    }
                }
                if (aux == D.Entidades.Count - 1)
                {
                    D.Entidades[aux - 1].Dir_SigEntidad = -1;
                    D.Entidades.RemoveAt(aux);
                }
                else if (aux == 0)
                {
                    D.Entidades.RemoveAt(aux);
                }
                else
                {
                    D.Entidades[aux - 1].Dir_SigEntidad = D.Entidades[aux + 1].Direccion_Entidad;
                    D.Entidades.RemoveAt(aux);
                }
                ReorganizaEntidades();
                ManejoArchivo.GuardaRelaciones(D.Entidades, RutaRelaciones);
            }
            else
            {
                MessageBox.Show("No se puede eliminar esta Tabla porque tiene una o más tablas dependientes");
            }

        }

        public void EliminaEntidad0(string NombreEntidad)
        {
            int aux = 0;
            for (int i = 0; i < D.Entidades.Count; i++)
            {
                if (D.Entidades[i].ObtenCadenaNombre() == NombreEntidad)
                {
                    aux = i;
                }
            }
            D.Entidades.RemoveAt(aux);
            ManejoArchivo.ActualizaCabecera(RutaArchivo, archivo, -1);
            Cabecera = ManejoArchivo.ObtenCabecera(RutaArchivo, archivo);
            LB_Cabecera.Text = Cabecera.ToString();
            ManejoArchivo.CargaRelaciones(D.Entidades, archivo, RutaRelaciones);
            ActualizaDGVEntidades();
        }
        public void ReorganizaEntidades()
        {
            D.Entidades = D.Entidades.OrderBy(o => o.ObtenCadenaNombre()).ToList();
            ManejoArchivo.ActualizaCabecera(RutaArchivo, archivo, D.Entidades[0].Direccion_Entidad);
            Cabecera = ManejoArchivo.ObtenCabecera(RutaArchivo, archivo);
            LB_Cabecera.Text = Cabecera.ToString();
            ActEntidades();
            ActualizaDGVEntidades();
        }
        private void AbrirArchivoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog();
            VentanaAbrir.InitialDirectory = "c:\\Users\\narut\\OneDrive\\Proyecto Archivos\\bin\\Debug";
            VentanaAbrir.Filter = "Diccionario (*.d)|*.d";
            if (VentanaAbrir.ShowDialog() == DialogResult.OK)
            {
                RutaArchivo = VentanaAbrir.FileName;
                string[] Cad = VentanaAbrir.FileName.Split('\\');
                RutaCarpeta = "";
                for (int i = 0; i < Cad.Length - 1; i++)
                {
                    RutaCarpeta += Cad[i] + "\\";
                }
                RutaRelaciones = RutaCarpeta + "\\Relaciones";
                MessageBox.Show("Ruta Archivo :" + RutaArchivo);
                Cabecera = ManejoArchivo.ObtenCabecera(RutaArchivo, archivo);
                D.Entidades = ManejoArchivo.LeeArchivo(Cabecera, RutaArchivo, archivo);
                ManejoArchivo.CargaRelaciones(D.Entidades, archivo, RutaRelaciones);
                ActualizaDGVEntidades();
                LB_Cabecera.Text = Cabecera.ToString();
                label1.Text = RutaArchivo;
            }
        }

        public List<string> ObtenRutaCarpetaArchivo(string RutaCompleta)
        {
            string[] Lista = RutaCompleta.Split('\\');
            string CadenaAux = Lista[Lista.Length - 1];
            string CadenaAux2 = "";
            for (int i = 0; i < RutaCompleta.Length - 2; i++)
            {
                CadenaAux2 += RutaCompleta[i];
            }
            List<string> ListaS = new List<string>();
            ListaS.Add(CadenaAux2);
            string RutaA = CadenaAux2 + "\\" + CadenaAux;
            ListaS.Add(RutaA);
            return ListaS;
        }
        private void NuevoArchivoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog VentanaGuardar = new SaveFileDialog();
                VentanaGuardar.Filter = "Diccionario (*.d)|*.d";
                if (VentanaGuardar.ShowDialog() == DialogResult.OK)
                {
                    List<string> Rutas = ObtenRutaCarpetaArchivo(VentanaGuardar.FileName);
                    RutaCarpeta = Rutas.ElementAt(Rutas.Count - 2);
                    RutaArchivo = Rutas.ElementAt(Rutas.Count - 1);
                    MessageBox.Show("Ruta Archivo: " + RutaArchivo);
                    if (!Directory.Exists(RutaCarpeta))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(RutaCarpeta);
                        MessageBox.Show("Se creó correctamente la carpeta");
                        RutaRelaciones = RutaCarpeta + "\\Relaciones";
                    }
                    ManejoArchivo.CreaArchivo(RutaArchivo, archivo);
                    ManejoArchivo.CreaCabecera(RutaArchivo, archivo);
                    label1.Text = RutaArchivo.ToString();
                    Cabecera = ManejoArchivo.ObtenCabecera(RutaArchivo, archivo);
                    LB_Cabecera.Text = Cabecera.ToString();
                }
            }
            catch
            {

            }
        }
        public void ActEntidades()
        {
            for (int i = 0; i < D.Entidades.Count - 1; i++)
            {
                D.Entidades[i].Dir_SigEntidad = D.Entidades[i + 1].Direccion_Entidad;
                ManejoArchivo.ModificaEntidad(D.Entidades[i], RutaArchivo, archivo);
            }
            D.Entidades[D.Entidades.Count - 1].Dir_SigEntidad = -1;
            ManejoArchivo.ModificaEntidad(D.Entidades[D.Entidades.Count - 1], RutaArchivo, archivo);
        }
        private void ActualizaDGVEntidades()
        {
            DGV_Entidades.Rows.Clear();
            ComboB_EntidadAtributo.Items.Clear();
            ComboB_EntidadRegistros.Items.Clear();
            ComboB_EntidadRelaciones.Items.Clear();
            foreach (Entidad e in D.Entidades)
            {
                ComboB_EntidadAtributo.Items.Add(e.ObtenCadenaNombre());
                ComboB_EntidadRegistros.Items.Add(e.ObtenCadenaNombre());
                ComboB_EntidadRelaciones.Items.Add(e.ObtenCadenaNombre());
                DGV_Entidades.Rows.Add(e.AuxClave, e.ObtenCadenaNombre(), e.Direccion_Entidad, e.Direccion_Atributo, e.Direccion_Dato, e.Dir_SigEntidad);
            }
        }

        private void ActualizaDGVAtributos(Entidad e)
        {
            DGV_Atributos.Rows.Clear();
            foreach (Atributo a in e.Atributos)
            {
                DGV_Atributos.Rows.Add(a.AuxClave, a.ObtenCadenaNombre(), a.getTipoAtributo(), a.Longitud, a.Direccion_Atributo, a.Tipo_Indice, a.Tipo_Llave, a.Direccion_Indice, a.Direccion_SiguienteAtributo);
            }
        }

        private void ActualizaDGVRelaciones(Entidad e)
        {
            DGV_Relaciones.Rows.Clear();
            foreach (Relacion r in e.Relaciones)
            {
                DGV_Relaciones.Rows.Add(e.ObtenCadenaNombre(), r.NombreAtributoTablaHijo, r.NombreTablaPadre, r.NombreAtributoTablaPadre);
            }
        }

        private void ActualizaApuntadoresAtributos(Entidad e)
        {
            if (e.Atributos.Count >= 1)
            {
                for (int i = 0; i < e.Atributos.Count - 1; i++)
                {
                    e.Atributos[i].Direccion_SiguienteAtributo = e.Atributos[i + 1].Direccion_Atributo;
                    ManejoArchivo.ModificaAtributo(e.Atributos[i], RutaArchivo, archivo);
                }
                e.Direccion_Atributo = e.Atributos[0].Direccion_Atributo;
                e.Atributos[e.Atributos.Count - 1].Direccion_SiguienteAtributo = -1;
                ManejoArchivo.ModificaAtributo(e.Atributos[e.Atributos.Count - 1], RutaArchivo, archivo);
                ManejoArchivo.ModificaEntidad(e, RutaArchivo, archivo);
            }
            else
            {
                e.Direccion_Atributo = -1;
                ManejoArchivo.ModificaEntidad(e, RutaArchivo, archivo);
            }

        }

        private void ComboB_TipoAtributo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboB_TipoAtributo.SelectedIndex == 0)
            {
                TextB_LongitudAtributo.Text = "4";
                TextB_LongitudAtributo.Enabled = false;
            }
            else if (ComboB_TipoAtributo.SelectedIndex == 1)
            {
                TextB_LongitudAtributo.Text = "";
                TextB_LongitudAtributo.Enabled = true;
            }
            else if (ComboB_TipoAtributo.SelectedIndex == 2)
            {
                TextB_LongitudAtributo.Text = "8";
                TextB_LongitudAtributo.Enabled = false;
            }
        }
        private void AgregaAtributo()
        {
            try
            {
                bool NuevoAtributo = true;
                if ((String.IsNullOrEmpty(TextB_NombreAtributo.Text) || string.IsNullOrEmpty(TextB_LongitudAtributo.Text)))
                {
                    MessageBox.Show("Complete los datos requeridos por favor");
                }
                else
                {
                    foreach (Atributo a in D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex).Atributos)
                    {
                        if (a.ObtenCadenaNombre() == TextB_NombreAtributo.Text)
                        {
                            NuevoAtributo = false;
                            MessageBox.Show("Ya existe un atributo con el mismo nombre en la entidad: " +
                            D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex).ObtenCadenaNombre() + ", intente de nuevo con otro nombre");
                            TextB_NombreAtributo.Clear();
                        }
                    }
                    if ((ObtenIndexLlaves(D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex), 'P') != -1) && CB_TipoLlave.Text[0] == 'P')
                    {
                        NuevoAtributo = false;
                        MessageBox.Show("Ya existe un atributo llave Primaria en esta Tabla");
                    }
                    if (NuevoAtributo == true) // En caso de que no exista un atributo con el mismo nombre entramos aquí.
                    {
                        Atributo nuevo;
                        byte[] Clave = ObtenClaveAleatoria();
                        char[] NombreAtributo = ConvierteCadena(TextB_NombreAtributo.Text, 35);
                        char TipoDato = ComboB_TipoAtributo.Text[0];
                        Int32 Longitud = Convert.ToInt32(TextB_LongitudAtributo.Text);
                        Int32 TipoIndice = Convert.ToInt32(ComboB_TipoIndiceAtributo.SelectedIndex);
                        long DireccionAtributo = ManejoArchivo.ObtenUltimaDireccion(RutaArchivo, archivo);
                        char TipoLlave = CB_TipoLlave.Text[0];
                        nuevo = new Atributo(Clave, NombreAtributo, TipoDato, Longitud, DireccionAtributo, TipoIndice, -1, -1, TipoLlave);
                        if (TipoLlave == 'S')
                        {
                            AgregaRelacion(D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex), nuevo);
                        }
                        if (TipoLlave == 'P' && nuevo.Tipo_Atributo != 'E')
                        {
                            MessageBox.Show("¡La llave primaria solamente puede ser de tipo Entero!");
                        }
                        else
                        {
                            ManejoArchivo.AgregaAtributo(nuevo, RutaArchivo, archivo);
                            D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex).Atributos.Add(nuevo);
                            ActualizaApuntadoresAtributos(D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex));
                            ActualizaDGVAtributos(D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex));
                            TextB_NombreAtributo.Clear();
                            TextB_LongitudAtributo.Clear();
                            TextB_LongitudAtributo.Text = "4";
                        }

                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        public void AgregaRelacion(Entidad EntidadHijo, Atributo Nuevo)
        {
            Atributo Aux = FormAtributoFK.ObtenAtributo();
            Entidad EntidadPadre = FormAtributoFK.ObtenEntidad();
            if (Aux != null & EntidadPadre != null)
            {
                Nuevo.Tipo_Atributo = Aux.Tipo_Atributo;
                Nuevo.Longitud = Aux.Longitud;
                EntidadHijo.Relaciones.Add(new Relacion(EntidadPadre.ObtenCadenaNombre(), Aux.ObtenCadenaNombre(), Nuevo.ObtenCadenaNombre()));
                string CadenaMensaje = "Nueva Relacion:\nEntidad Padre: " + EntidadPadre.ObtenCadenaNombre() + "\nNombre Atributo Entidad Padre: " + Aux.ObtenCadenaNombre() + "\nEntidad Hijo" +
                   EntidadHijo.ObtenCadenaNombre() + "\nNombre Atributo Hijo: " + Nuevo.ObtenCadenaNombre();
                MessageBox.Show(CadenaMensaje);
                ManejoArchivo.GuardaRelaciones(D.Entidades, RutaRelaciones);
            }
        }


        private void ModificaAtributo() // Nombre, tipo de dato, longitud, Tipo Indice
        {
            try
            {
                Entidad aux = D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex);
                foreach (Atributo a in aux.Atributos)
                {
                    if (a.ObtenCadenaNombre() == AuxBusqueda)
                    {
                        if ((ObtenIndexLlaves(aux, 'P') != -1) && CB_TipoLlave.Text[0] == 'P' && (a.Tipo_Llave != 'P'))
                        {
                            MessageBox.Show("Ya existe un atributo llave Primaria en esta Tabla, intente de nuevo");
                        }
                        else
                        {
                            char[] NombreAtributo = ConvierteCadena(TextB_NombreAtributo.Text, 35);
                            char TipoDato = ComboB_TipoAtributo.Text[0];
                            Int32 Longitud = Convert.ToInt32(TextB_LongitudAtributo.Text);
                            Int32 TipoIndice = Convert.ToInt32(ComboB_TipoIndiceAtributo.SelectedIndex);
                            char TipoLlave;
                            char TipoLlaveAnterior = a.Tipo_Llave;
                            string NombreAtributoAnterior = a.ObtenCadenaNombre();
                            if (CB_TipoLlave.SelectedIndex == 0)
                            {
                                TipoLlave = 'P';
                            }
                            else if (CB_TipoLlave.SelectedIndex == 1)
                            {
                                TipoLlave = 'S';
                            }
                            else
                            {
                                TipoLlave = 'N';
                            }

                            a.Nombre_Atributo = NombreAtributo;
                            a.Tipo_Atributo = TipoDato;
                            a.Longitud = Longitud;
                            a.Tipo_Indice = TipoIndice;
                            a.Tipo_Llave = TipoLlave;

                            if (TipoLlaveAnterior == 'S' && TipoLlave != 'S') // Aquí eliminamos la relación
                            {
                                EliminaRelacion(aux, NombreAtributoAnterior);
                            }
                            else if (TipoLlaveAnterior == 'S' && TipoLlave == 'S')// Aquí solo modificamos la relación
                            {
                                EliminaRelacion(aux, NombreAtributoAnterior);
                                a.Longitud = 4;
                                a.Tipo_Atributo = 'E';
                                AgregaRelacion(aux, a);
                            }
                            ManejoArchivo.ModificaAtributo(a, RutaArchivo, archivo);
                            ActualizaApuntadoresAtributos(D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex));
                            ActualizaDGVAtributos(D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex));
                            TextB_NombreAtributo.Clear();
                        }
                    }
                }
            }
            catch
            {

            }
        }
        public void EliminaRelacion(Entidad EntidadHijo, string AtributoHijo)
        {
            for (int i = 0; i < EntidadHijo.Relaciones.Count; i++)
            {
                if (EntidadHijo.Relaciones[i].NombreAtributoTablaHijo == AtributoHijo)
                {
                    EntidadHijo.Relaciones.RemoveAt(i);
                    break;
                }
            }
            ManejoArchivo.GuardaRelaciones(D.Entidades, RutaRelaciones);
        }
        private void EliminaAtributo()
        {
            Entidad aux = D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex);
            for (int i = 0; i < aux.Atributos.Count; i++)
            {
                if (aux.Atributos[i].ObtenCadenaNombre() == TextB_NombreAtributo.Text)
                {
                    aux.Atributos.RemoveAt(i);
                    EliminaRelacion(aux, TextB_NombreAtributo.Text);
                }
            }
            ActualizaApuntadoresAtributos(aux);
            ActualizaDGVAtributos(D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex));
        }
        public char[] ConvierteCadena(string Cadena, int tamaño)
        {
            char[] Auxiliar = new char[tamaño];
            int cont = 0;
            foreach (var c in Cadena)
            {
                Auxiliar[cont] = c;
                cont++;
            }
            return Auxiliar;
        }
        public byte[] ObtenClaveAleatoria()
        {
            byte[] buffer = new byte[5];
            Random random = new Random();
            random.NextBytes(buffer);
            return buffer;
        }

        private void BT_AgregarAtributo_Click_1(object sender, EventArgs e)
        {
            if (File.Exists(RutaArchivoDatos) && (ManejoArchivo.ObtenUltimaDireccion(RutaArchivoDatos, archivo) != 0))
            {
                MessageBox.Show("No se puede modificar la tabla porque ya existen registros en ella");
                TextB_NombreAtributo.Text = "";
                TextB_LongitudAtributo.Text = "";
                CB_TipoLlave.SelectedIndex = -1;
            }
            else
            {
                switch (ComboB_AccionesAtributos.SelectedIndex)
                {
                    case 0:
                        AgregaAtributo();
                        break;
                    case 1:
                        if (busqueda == true)
                        {
                            ModificaAtributo();
                        }
                        break;
                    case 2:
                        EliminaAtributo();
                        break;
                }
            }
        }

        private void BT_BusquedaAtributo_Click(object sender, EventArgs e)
        {
            Entidad aux = D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex);
            foreach (Atributo a in aux.Atributos)
            {
                if (a.ObtenCadenaNombre() == TextB_NombreAtributo.Text)
                {
                    AuxBusqueda = TextB_NombreAtributo.Text;
                    ComboB_TipoAtributo.SelectedItem = a.Tipo_Atributo.ToString();
                    ComboB_TipoIndiceAtributo.SelectedIndex = a.Tipo_Indice;
                    TextB_LongitudAtributo.Text = a.Longitud.ToString();
                    busqueda = true;
                }
            }
        }

        private void ComboB_EntidadAtributo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActualizaDGVAtributos(D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex));
        }

        private void EstadoInicial()
        {
            RutaArchivo = "";
            RutaArchivoDatos = "";
            RutaArchivoIndicePrimario = "";
            RutaArchivoIndiceSecundario = "";
            LB_EntidadIndice.Text = "";
            D = new Diccionario();
            DGV_Atributos.Rows.Clear();
            DGV_AtributosRegistro.Rows.Clear();
            DGV_Registros.Rows.Clear();
            DGV_Registros.Columns.Clear();
            DGV_AtributosRegistro.Columns.Clear();
            DGV_IndicePrimario.Rows.Clear();
            DGV_ArbolPrimarioBloquePrincipal.Rows.Clear();
            DGV_ArbolSSecundario.Rows.Clear();
            DGV_ArbolSecundario.Rows.Clear();
            DGV_BloquePrincipalIndiceSecundario.Rows.Clear();
            DGV_CajonIndiceSecundario.Rows.Clear();
            ActualizaDGVEntidades();
            Cabecera = -1;
            LB_Cabecera.Text = Cabecera.ToString();
            label1.Text = RutaArchivo;
            ComboB_AccionesAtributos.SelectedIndex = 0;
            ComboB_EntidadRegistros.Items.Clear();
            ComboB_EntidadRegistros.Text = "";
            TextB_LongitudAtributo.Clear();
            TextB_LongitudAtributo.Text = "4";
            TextB_NombreActualEnt.Clear();
            TextB_NombreAtributo.Clear();
            ComboB_TipoAtributo.SelectedIndex = 0;
        }

        private void CerrarArchivoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EstadoInicial();
        }

        private void ComboB_EntidadRegistros_SelectedIndexChanged(object sender, EventArgs e)
        {
            indiceEntidadRegistro = ComboB_EntidadRegistros.SelectedIndex;
            Entidad Aux = D.Entidades.ElementAt(indiceEntidadRegistro);
            LB_EntidadIndice.Text = Aux.ObtenCadenaNombre();
            RutaArchivoDatos = RutaCarpeta + "\\" + Aux.ClaveEnString() + ".dat";
            ActualizaDGVRegistros(Aux);
            ObtenRutasIndicesEntidad(Aux);
            int IndicePrimario = ObtenIndex(Aux, 2);
            int IndiceSecundario = ObtenIndex(Aux, 3);
            if (IndicePrimario != -1)
            {
                ActualizaDGVIndicePrimario(Aux);
            }
            if (IndiceSecundario != -1)
            {
                ActualizaDGVBloquePrincipalIndiceSecundario(Aux);
            }
            if (ObtenIndex(Aux, 4) != -1)
            {
                ActualizaDGVArbolPrimario(Aux);
            }
            if (ObtenIndex(Aux, 5) != -1)
            {
                ActualizaDGVArbolSecundario(Aux);
            }
        }
        private void ActualizaDGVRegistros(Entidad E)
        {
            try
            {
                DGV_AtributosRegistro.Columns.Clear();
                DGV_AtributosRegistro.Rows.Clear();
                DGV_Registros.Columns.Clear();
                DGV_Registros.Rows.Clear();
                if (!File.Exists(RutaArchivoDatos))
                {
                    ManejoArchivo.CreaArchivo(RutaArchivoDatos, archivo);
                }
                else if (E.Direccion_Dato == -1)
                {
                    ManejoArchivo.CreaArchivo(RutaArchivoDatos, archivo);
                }
                ManejoArchivo.LeeDatos(E, RutaArchivoDatos, archivo);
                DGV_Registros.Columns.Add("Dirección", "Dirección");
                foreach (Atributo a in E.Atributos)
                {
                    DGV_AtributosRegistro.Columns.Add(a.ObtenCadenaNombre(), a.ObtenCadenaNombre());
                    DGV_Registros.Columns.Add(a.ObtenCadenaNombre(), a.ObtenCadenaNombre());
                }
                DGV_AtributosRegistro.Rows.Add();
                DGV_Registros.Columns.Add("Dirección Sig", "Dirección Sig");
                int j = 0;
                int i = 0;
                if (E.Registros.Count == 0)
                {
                    E.Direccion_Dato = -1;
                }
                foreach (Registro R in E.Registros)
                {
                    DGV_Registros.Rows.Add();
                    DGV_Registros.Rows[j].Cells["Dirección"].Value = R.Direccion;
                    DGV_Registros.Rows[j].Cells["Dirección Sig"].Value = R.DireccionSiguiente;
                    foreach (byte[] b in R.Informacion)
                    {
                        if (E.Atributos[i].getTipoAtributo() == "E")
                        {
                            int Numero = BitConverter.ToInt32(b, 0);
                            DGV_Registros.Rows[j].Cells[E.Atributos[i].ObtenCadenaNombre()].Value = Numero;
                        }
                        else if (E.Atributos[i].getTipoAtributo() == "C")
                        {
                            string Cadena = System.Text.Encoding.UTF8.GetString(b);
                            DGV_Registros.Rows[j].Cells[E.Atributos[i].ObtenCadenaNombre()].Value = Cadena;
                        }
                        else if (E.Atributos[i].getTipoAtributo() == "D")
                        {
                            double Numero = BitConverter.ToDouble(b, 0);
                            DGV_Registros.Rows[j].Cells[E.Atributos[i].ObtenCadenaNombre()].Value = Numero;
                        }
                        i++;
                        if (i >= E.Atributos.Count)
                        {
                            i = 0;
                        }
                    }
                    j++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        private void DGV_AtributosRegistro_SelectionChanged(object sender, EventArgs e)
        {
            DGV_AtributosRegistro.BeginEdit(true);
        }

        private void BT_AgregarRegistro_Click(object sender, EventArgs e)
        {
            Entidad Aux = D.Entidades.ElementAt(indiceEntidadRegistro);
            Registro NuevoRegistro = ObtenNuevoRegistro(Aux);
            int indicePrimario = ObtenIndex(Aux, 2);
            int indiceSecundario = ObtenIndex(Aux, 3);
            int indiceArbolPrimario = ObtenIndex(Aux, 4);
            int indiceArbolSecundario = ObtenIndex(Aux, 5);
            int LlaveP = ObtenIndexLlaves(Aux, 'P');
            bool Nuevo = true;
            bool RegistroValido = true;
            if (indicePrimario != -1)
            {
                RegistroValido = CompruebaSiExisteClaveRepetida(Aux, indicePrimario, NuevoRegistro);
                if (!RegistroValido)
                {
                    MessageBox.Show("La clave primaria se encuentra repetida ");
                }
            }
            if (LlaveP != -1)
            {
                RegistroValido = CompruebaSiExisteClaveRepetida(Aux, LlaveP, NuevoRegistro);
                if (!RegistroValido)
                {
                    MessageBox.Show("La llave Primaria está repetida, intente de nuevo");
                }
            }
            if (indiceArbolPrimario != -1)
            {
                Arbol Primario = new Arbol(ManejoArchivo.ObtenNodos(Aux.Atributos[indiceArbolPrimario], archivo, RutaArchivoArbolPrimario), Aux.Atributos[indiceArbolPrimario]);
                if (Primario.ContieneClaveEnHojas(BitConverter.ToInt32(NuevoRegistro.Informacion[indiceArbolPrimario], 0)))
                {
                    MessageBox.Show("Esta clave ya existe en el arbol");
                    Nuevo = false;
                }
            }
            if (ObtenIndexLlaves(Aux, 'S') != -1)
            {
                Nuevo = CompruebaIntegridadReferencialInsercionTablaDependiente(Aux, NuevoRegistro);
                if (!Nuevo)
                {
                    MessageBox.Show("No cumple Integridad Referencial al insertar un registro");
                }
            }
            if (Nuevo && RegistroValido)
            {
                Aux.Registros.Add(NuevoRegistro);
                int index = 0;
                bool Cve_Busqueda = false;
                foreach (Atributo a in Aux.Atributos)
                {
                    if (a.getTipoIndice() == "1")
                    {
                        Cve_Busqueda = true;
                        break;
                    }
                    else
                    {
                        index++;
                    }
                }
                if (indicePrimario != -1)
                {
                    int tipo = 0;
                    if (Aux.Atributos[indicePrimario].getTipoAtributo() == "C")
                    {
                        tipo = 2;
                    }
                    else if (Aux.Atributos[indicePrimario].getTipoAtributo() == "E")
                    {
                        tipo = 1;
                    }
                    else if (Aux.Atributos[indicePrimario].getTipoAtributo() == "D")
                    {
                        tipo = 3;
                    }
                    Indice NuevoIndice = new Indice();
                    NuevoIndice.Llave = NuevoRegistro.Informacion[indicePrimario];
                    NuevoIndice.Desplazamiento = NuevoRegistro.Direccion;
                    int Longitud = Aux.Atributos[indicePrimario].Longitud;
                    List<Indice> Indices = ManejoArchivo.LeeIndiceBloquePrincipal(RutaArchivoIndicePrimario, archivo, tipo, Longitud);
                    Indices.Add(NuevoIndice);
                    List<Indice> ListaOrdenada = OrdenaIndices(Indices, tipo);
                    EscribeIndices(ListaOrdenada, tipo, RutaArchivoIndicePrimario);
                    ActualizaDGVIndicePrimario(Aux);
                }
                if (indiceSecundario != -1)
                {
                    AgregaIndiceSecundario(Aux, indiceSecundario, NuevoRegistro);
                }

                if (indiceArbolPrimario != -1)
                {
                    ManejadorArboles.InsertaEnArbolPrimario(Aux.Atributos[indiceArbolPrimario], RutaArchivoArbolPrimario, BitConverter.ToInt32(NuevoRegistro.Informacion[indiceArbolPrimario], 0), NuevoRegistro.Direccion, RutaArchivo);
                    Arbol ArbolP = new Arbol(ManejoArchivo.ObtenNodos(Aux.Atributos[indiceArbolPrimario], archivo, RutaArchivoArbolPrimario), Aux.Atributos[indiceArbolPrimario]);
                    ActualizaDGVArbolPrimario(Aux);
                }
                if (indiceArbolSecundario != -1)
                {
                    ManejadorArboles.InsertaEnArbolSecundario(Aux.Atributos[indiceArbolSecundario], RutaArchivoArbolSecundario, BitConverter.ToInt32(NuevoRegistro.Informacion[indiceArbolSecundario], 0), NuevoRegistro.Direccion, RutaArchivo);
                    ActualizaDGVArbolSecundario(Aux);
                }
                if (Cve_Busqueda)
                {
                    OrganizaRegistrosOrdenado(Aux, index);
                    ActualizaDGVRegistros(Aux);
                    ActualizaDGVEntidades();
                }
                else
                {
                    OrganizaRegistros(Aux);
                    ActualizaDGVRegistros(Aux);
                    ActualizaDGVEntidades();
                }
            }
            else
            {
                MessageBox.Show("No se pudo agregar el registro a la tabla");
                ActualizaDGVRegistros(Aux);
            }
        }

        private bool CompruebaSiExisteClaveRepetida(Entidad Aux, int Indice, Registro NuevoRegistro)
        {
            bool Nuevo = true;
            for (int i = 0; i < Aux.Registros.Count; i++)
            {
                if (Aux.Atributos[Indice].getTipoAtributo() == "E")
                {
                    int Numero = BitConverter.ToInt32(Aux.Registros[i].Informacion[Indice], 0);
                    int Numero2 = BitConverter.ToInt32(NuevoRegistro.Informacion[Indice], 0);
                    if (Numero == Numero2)
                    {
                        Nuevo = false;
                    }
                }
                else if (Aux.Atributos[Indice].getTipoAtributo() == "C")
                {
                    string Cadena = Encoding.UTF8.GetString(Aux.Registros[i].Informacion[Indice], 0, Aux.Atributos[Indice].Longitud);
                    string Cadena2 = Encoding.UTF8.GetString(NuevoRegistro.Informacion[Indice], 0, Aux.Atributos[Indice].Longitud);
                    if (Cadena.Equals(Cadena2))
                    {
                        Nuevo = false;
                    }
                }
                else if (Aux.Atributos[Indice].getTipoAtributo() == "D")
                {
                    double Numero = BitConverter.ToDouble(Aux.Registros[i].Informacion[Indice], 0);
                    double Numero2 = BitConverter.ToDouble(NuevoRegistro.Informacion[Indice], 0);
                    if (Numero == Numero2)
                    {
                        Nuevo = false;
                    }
                }
            }
            return Nuevo;
        }
        private void ActualizaDGVIndicePrimario(Entidad aux)
        {
            DGV_IndicePrimario.Rows.Clear();
            int IndicePrimario = ObtenIndex(aux, 2);
            int TipoDatoIndice = 0;
            if (aux.Atributos[IndicePrimario].getTipoAtributo() == "E")
            {
                TipoDatoIndice = 1;
            }
            else if (aux.Atributos[IndicePrimario].getTipoAtributo() == "C")
            {
                TipoDatoIndice = 2;
            }
            else if (aux.Atributos[IndicePrimario].getTipoAtributo() == "D")
            {
                TipoDatoIndice = 3;
            }
            int Longitud = aux.Atributos[IndicePrimario].Longitud;
            List<Indice> Indices = ManejoArchivo.LeeIndiceBloquePrincipal(RutaArchivoIndicePrimario, archivo, TipoDatoIndice, Longitud);
            if (Indices.Count > 0)
            {
                aux.Atributos[IndicePrimario].Direccion_Indice = 0;
            }
            ManejoArchivo.ModificaAtributo(aux.Atributos[IndicePrimario], RutaArchivo, archivo);
            int j = 0;
            foreach (Indice i in Indices)
            {
                DGV_IndicePrimario.Rows.Add();
                if (aux.Atributos[IndicePrimario].getTipoAtributo() == "E")
                {
                    int Numero = BitConverter.ToInt32(i.Llave, 0);
                    DGV_IndicePrimario.Rows[j].Cells["Llave"].Value = Numero;
                }
                else if (aux.Atributos[IndicePrimario].getTipoAtributo() == "C")
                {
                    string Cadena = System.Text.Encoding.UTF8.GetString(i.Llave);
                    DGV_IndicePrimario.Rows[j].Cells["Llave"].Value = Cadena;
                }
                else if (aux.Atributos[IndicePrimario].getTipoAtributo() == "D")
                {
                    float Numero = BitConverter.ToSingle(i.Llave, 0);
                    DGV_IndicePrimario.Rows[j].Cells["Llave"].Value = Numero;
                }
                DGV_IndicePrimario.Rows[j].Cells["Dirección"].Value = i.Desplazamiento;
                j++;
            }
        }

        private void ActualizaDGVBloquePrincipalIndiceSecundario(Entidad aux)
        {
            DGV_BloquePrincipalIndiceSecundario.Rows.Clear();
            DGV_CajonIndiceSecundario.Rows.Clear();
            int IndiceSecundario = ObtenIndex(aux, 3);
            int j = 0;
            if (IndiceSecundario != -1)
            {
                List<Indice> IndicesSecundarios = ManejoArchivo.LeeIndiceBloquePrincipal(RutaArchivoIndiceSecundario, archivo, ObtenTipoDatoAtributo(aux.Atributos[IndiceSecundario]), aux.Atributos[IndiceSecundario].Longitud);
                foreach (Indice i in IndicesSecundarios)
                {
                    DGV_BloquePrincipalIndiceSecundario.Rows.Add();
                    if (aux.Atributos[IndiceSecundario].getTipoAtributo() == "E")
                    {
                        int Numero = BitConverter.ToInt32(i.Llave, 0);
                        DGV_BloquePrincipalIndiceSecundario.Rows[j].Cells["LlaveSecundaria"].Value = Numero;
                    }
                    else if (aux.Atributos[IndiceSecundario].getTipoAtributo() == "C")
                    {
                        string Cadena = System.Text.Encoding.UTF8.GetString(i.Llave);
                        //MessageBox.Show("Apellido: " + Cadena);
                        DGV_BloquePrincipalIndiceSecundario.Rows[j].Cells["LlaveSecundaria"].Value = Cadena;
                    }
                    DGV_BloquePrincipalIndiceSecundario.Rows[j].Cells["DireccionCajonSecundario"].Value = i.Desplazamiento;
                    j++;
                }
            }
        }
        private void OrganizaRegistros(Entidad E)
        {
            try
            {
                if (E.Registros.Count >= 1)
                {
                    for (int i = 0; i < E.Registros.Count - 1; i++)
                    {
                        E.Registros[i].DireccionSiguiente = E.Registros[i + 1].Direccion;
                    }
                    E.Registros[E.Registros.Count - 1].DireccionSiguiente = -1;
                    E.Direccion_Dato = E.Registros[0].Direccion;
                    ManejoArchivo.ModificaEntidad(E, RutaArchivo, archivo);
                    foreach (Registro r in E.Registros)
                    {
                        ManejoArchivo.ModificaRegistro(r, RutaArchivoDatos, archivo);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        private void OrganizaRegistrosOrdenado(Entidad E, int index)
        {
            try
            {
                if (E.Registros.Count >= 1)
                {
                    if (E.Atributos[index].getTipoAtributo() == "C")
                    {
                        E.Registros = E.Registros.OrderBy(o => System.Text.Encoding.UTF8.GetString(o.Informacion[index])).ToList();
                    }
                    else if (E.Atributos[index].getTipoAtributo() == "E")
                    {
                        E.Registros = E.Registros.OrderBy(o => BitConverter.ToInt32(o.Informacion[index], 0)).ToList();
                    }
                    else if (E.Atributos[index].getTipoAtributo() == "D")
                    {
                        E.Registros = E.Registros.OrderBy(o => BitConverter.ToDouble(o.Informacion[index], 0)).ToList();
                    }
                    for (int i = 0; i < E.Registros.Count - 1; i++)
                    {
                        E.Registros[i].DireccionSiguiente = E.Registros[i + 1].Direccion;
                    }
                    E.Registros[E.Registros.Count - 1].DireccionSiguiente = -1;
                    E.Direccion_Dato = E.Registros[0].Direccion;
                    ManejoArchivo.ModificaEntidad(E, RutaArchivo, archivo);
                    foreach (Registro R in E.Registros)
                    {
                        ManejoArchivo.ModificaRegistro(R, RutaArchivoDatos, archivo);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void ObtenRutasIndicesEntidad(Entidad aux)
        {
            IndexPrimario = ObtenIndex(aux, 2);
            IndexSecundario = ObtenIndex(aux, 3);
            IndexArbolPrimario = ObtenIndex(aux, 4);
            IndexArbolSecundario = ObtenIndex(aux, 5);
            if (IndexPrimario != -1)
            {
                RutaArchivoIndicePrimario = RutaCarpeta + "\\" + aux.Atributos[IndexPrimario].ClaveEnString() + ".idx";
                if (!File.Exists(RutaArchivoIndicePrimario) || aux.Atributos[IndexPrimario].Direccion_Indice == -1)
                {
                    //MessageBox.Show("Estoy creando el archivo de Indice Primario");
                    ManejoArchivo.CreaArchivo(RutaArchivoIndicePrimario, archivo);
                    ManejoArchivo.EscribeBytesInicialesArchivoIndice(RutaArchivoIndicePrimario, archivo, 0);
                }
            }
            if (IndexSecundario != -1)
            {
                RutaArchivoIndiceSecundario = RutaCarpeta + "\\" + aux.Atributos[IndexSecundario].ClaveEnString() + ".idx";
                if (!File.Exists(RutaArchivoIndiceSecundario) || aux.Atributos[IndexSecundario].Direccion_Indice == -1)
                {
                    ManejoArchivo.CreaArchivo(RutaArchivoIndiceSecundario, archivo);
                    ManejoArchivo.EscribeBytesInicialesArchivoIndice(RutaArchivoIndiceSecundario, archivo, 0);
                }
            }

            if (IndexArbolPrimario != -1)
            {
                RutaArchivoArbolPrimario = RutaCarpeta + "\\" + aux.Atributos[IndexArbolPrimario].ClaveEnString() + ".idx";
                if (!File.Exists(RutaArchivoArbolPrimario) || aux.Atributos[IndexArbolPrimario].Direccion_Indice == -1)
                {
                    MessageBox.Show("Creando el archivo: " + RutaArchivoArbolPrimario);
                    ManejoArchivo.CreaArchivo(RutaArchivoArbolPrimario, archivo);
                    aux.Atributos[IndexArbolPrimario].Direccion_Indice = -1;
                    ManejoArchivo.ModificaAtributo(aux.Atributos[IndexArbolPrimario], RutaArchivo, archivo);
                }
            }

            if (IndexArbolSecundario != -1)
            {
                RutaArchivoArbolSecundario = RutaCarpeta + "\\" + aux.Atributos[IndexArbolSecundario].ClaveEnString() + ".idx";
                if (!File.Exists(RutaArchivoArbolSecundario) || aux.Atributos[IndexArbolSecundario].Direccion_Indice == -1)
                {
                    MessageBox.Show("Creando el archivo: " + RutaArchivoArbolSecundario);
                    ManejoArchivo.CreaArchivo(RutaArchivoArbolSecundario, archivo);
                    aux.Atributos[IndexArbolSecundario].Direccion_Indice = -1;
                    MessageBox.Show(aux.Atributos[IndexArbolSecundario].Direccion_Indice.ToString());
                    ManejoArchivo.ModificaAtributo(aux.Atributos[IndexArbolSecundario], RutaArchivo, archivo);
                }
            }
        }
        private List<Indice> OrdenaIndices(List<Indice> Indices, int TipoDatoIndice)
        {
            switch (TipoDatoIndice)
            {
                case 1:
                    Indices = Indices.OrderBy(o => BitConverter.ToInt32(o.Llave, 0)).ToList();
                    break;
                case 2:
                    Indices = Indices.OrderBy(o => System.Text.Encoding.UTF8.GetString(o.Llave)).ToList();
                    break;
                case 3:
                    Indices = Indices.OrderBy(o => BitConverter.ToDouble(o.Llave, 0)).ToList();
                    break;
            }
            return Indices;
        }

        private void EscribeIndices(List<Indice> indices, int tipoDato, string RutaArchivoIndices)
        {
            ManejoArchivo.EscribeBloqueIndices(RutaArchivoIndicePrimario, archivo, indices, 0);
        }
        private int ObtenIndex(Entidad aux, int Tipo)
        {
            int Index = -1;
            for (int i = 0; i < aux.Atributos.Count; i++)
            {
                if (aux.Atributos[i].Tipo_Indice == Tipo)
                {
                    Index = i;
                }
            }
            return Index;
        }
        private void DGV_Registros_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            Entidad Aux = D.Entidades.ElementAt(indiceEntidadRegistro);
            indiceRegistroSeleccionado = DGV_Registros.CurrentRow.Index;
            foreach (Atributo A in Aux.Atributos)
            {
                DGV_AtributosRegistro.Rows[0].Cells[A.ObtenCadenaNombre()].Value = DGV_Registros.CurrentRow.Cells[A.ObtenCadenaNombre()].Value;
            }
        }

        private void BT_LimpiarDGVAtributosRegistro_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewCell dgvC in DGV_AtributosRegistro.Rows[0].Cells)
            {
                dgvC.Value = "";
            }
        }


        private void AgregaIndiceSecundario(Entidad Aux, int indiceSecundario, Registro NuevoRegistro)
        {
            Indice NuevoIndice = new Indice();
            NuevoIndice.Llave = NuevoRegistro.Informacion[indiceSecundario];
            List<Indice> IndicesSecundarios = ManejoArchivo.LeeIndiceBloquePrincipal(RutaArchivoIndiceSecundario, archivo, ObtenTipoDatoAtributo(Aux.Atributos[indiceSecundario])
                , Aux.Atributos[indiceSecundario].Longitud);
            Aux.Atributos[indiceSecundario].Direccion_Indice = 0;
            ManejoArchivo.ModificaAtributo(Aux.Atributos[indiceSecundario], RutaArchivo, archivo);
            if (IndicesSecundarios.Count > 0)
            {

                int indice = ObtenIndexListaIndices(NuevoIndice.Llave, ObtenTipoDatoAtributo(Aux.Atributos[indiceSecundario]), IndicesSecundarios);
                if (indice != -1)
                {
                    long Posicion = IndicesSecundarios[indice].Desplazamiento;
                    List<long> Direcciones = ManejoArchivo.LeeCajonIndiceSecundario(RutaArchivoIndiceSecundario, archivo, Posicion);
                    Direcciones.Add(NuevoRegistro.Direccion);
                    ManejoArchivo.EscribeCajonSecundario(RutaArchivoIndiceSecundario, archivo, Posicion, Direcciones);
                }
                else
                {
                    long Posicion = ManejoArchivo.ObtenUltimaDireccion(RutaArchivoIndiceSecundario, archivo);
                    long Direccion = NuevoRegistro.Direccion;
                    List<long> Direcciones = new List<long>();
                    Direcciones.Add(Direccion);
                    NuevoIndice.Desplazamiento = Posicion;
                    IndicesSecundarios.Add(NuevoIndice);
                    List<Indice> ListaOrdenada = OrdenaIndices(IndicesSecundarios, ObtenTipoDatoAtributo(Aux.Atributos[indiceSecundario]));
                    ManejoArchivo.EscribeBloqueIndices(RutaArchivoIndiceSecundario, archivo, ListaOrdenada, 0);
                    ManejoArchivo.EscribeCajonSecundario(RutaArchivoIndiceSecundario, archivo, Posicion, Direcciones);
                }
            }
            else
            {
                long Posicion = ManejoArchivo.ObtenUltimaDireccion(RutaArchivoIndiceSecundario, archivo);
                NuevoIndice.Desplazamiento = Posicion;
                List<long> Direcciones = new List<long>();
                Direcciones.Add(NuevoRegistro.Direccion);
                IndicesSecundarios.Add(NuevoIndice);
                ManejoArchivo.EscribeBloqueIndices(RutaArchivoIndiceSecundario, archivo, IndicesSecundarios, 0);
                ManejoArchivo.EscribeCajonSecundario(RutaArchivoIndiceSecundario, archivo, Posicion, Direcciones);
            }
            ActualizaDGVBloquePrincipalIndiceSecundario(Aux);
        }
        private void BT_ModificarRegistro_Click(object sender, EventArgs e)
        {
            bool Cve_Busqueda = false;
            int index = -1;
            Entidad Aux = D.Entidades.ElementAt(indiceEntidadRegistro);
            Registro RModificar = Aux.Registros.ElementAt(indiceRegistroSeleccionado);
            int PRIMARIA = ObtenIndex(Aux, 2);
            int SECUNDARIA = ObtenIndex(Aux, 3);
            int ArbolP = ObtenIndex(Aux, 4);
            int ArbolS = ObtenIndex(Aux, 5);
            Registro Nuevo = ObtenNuevoRegistro(Aux);
            Nuevo.Direccion = RModificar.Direccion;
            bool IntegridadReferencialCorrecta = true;
            bool RegistroValido = true;
            int IndexLlaveP = ObtenIndexLlaves(Aux, 'P');

            if (IndexLlaveP != -1)
            {
                RegistroValido = CompruebaSiExisteClaveRepetida(Aux, IndexLlaveP, Nuevo);
                if (BitConverter.ToInt32(RModificar.Informacion[IndexLlaveP], 0) == BitConverter.ToInt32(Nuevo.Informacion[IndexLlaveP], 0))
                {
                    RegistroValido = true;
                }
                if (!RegistroValido)
                {
                    MessageBox.Show("No se puede modificar el registro porque la nueva llave Primaria ya existe");
                }
            }
            if (RegistroValido)
            {
                if (ObtenIndexLlaves(Aux, 'S') != -1)
                {
                    IntegridadReferencialCorrecta = CompruebaIntegridadReferencialInsercionTablaDependiente(Aux, Nuevo);
                    if (!IntegridadReferencialCorrecta)
                    {
                        MessageBox.Show("No se cumple la Integridad Referencial al modificar el registro");
                    }
                }
                if (IntegridadReferencialCorrecta && BuscaRelacionTablaPadre(Aux.ObtenCadenaNombre())) // En caso de que esta tabla tenga Relacion con alguna otra tabla y sirva como su "Tabla Padre"
                {
                    MessageBox.Show("¡¡¡Modificando Llave Primaria en las tablas Dependientes!!!");
                    ActualizaClavePRegistrosHijos(Aux, BitConverter.ToInt32(Nuevo.Informacion[ObtenIndexLlaves(Aux, 'P')], 0), BitConverter.ToInt32(RModificar.Informacion[ObtenIndexLlaves(Aux, 'P')], 0));
                }
                if (IntegridadReferencialCorrecta && RegistroValido)
                {
                    if (PRIMARIA != -1)
                    {
                        byte[] LlavePrimaria = RModificar.Informacion[PRIMARIA];
                        List<Indice> Indices = ManejoArchivo.LeeIndiceBloquePrincipal(RutaArchivoIndicePrimario, archivo, ObtenTipoDatoAtributo(Aux.Atributos[PRIMARIA]), Aux.Atributos[PRIMARIA].Longitud);
                        Indices.RemoveAt(ObtenIndexListaIndices(LlavePrimaria, ObtenTipoDatoAtributo(Aux.Atributos[PRIMARIA]), Indices));
                        Indice NuevoIndice = new Indice();
                        NuevoIndice.Desplazamiento = RModificar.Direccion;
                        NuevoIndice.Llave = Nuevo.Informacion[PRIMARIA];
                        Indices.Add(NuevoIndice);
                        List<Indice> ListaOrdenada = OrdenaIndices(Indices, ObtenTipoDatoAtributo(Aux.Atributos[PRIMARIA]));
                        ManejoArchivo.EscribeBloqueIndices(RutaArchivoIndicePrimario, archivo, ListaOrdenada, 0);
                        ActualizaDGVIndicePrimario(Aux);
                    }
                    if (ArbolP != -1)
                    {
                        int LlavePrimaria = BitConverter.ToInt32(RModificar.Informacion[ArbolP], 0);
                        ArbolPrimario = new Arbol(ManejoArchivo.ObtenNodos(Aux.Atributos[ArbolP], archivo, RutaArchivoArbolPrimario), Aux.Atributos[ArbolP]);
                        ManejadorArboles.EliminaEnArbolPrimario(Aux.Atributos[ArbolP], RutaArchivoArbolPrimario, LlavePrimaria, RutaArchivo);
                        ManejadorArboles.InsertaEnArbolPrimario(Aux.Atributos[ArbolP], RutaArchivoArbolPrimario, BitConverter.ToInt32(Nuevo.Informacion[ArbolP], 0), Nuevo.Direccion, RutaArchivo);
                        ActualizaDGVArbolPrimario(Aux);
                    }
                    if (ArbolS != -1)
                    {
                        int LlaveeSecundaria = BitConverter.ToInt32(RModificar.Informacion[ArbolS], 0);
                        ManejadorArboles.EliminaEnArbolSecundario(Aux.Atributos[ArbolS], RutaArchivoArbolSecundario, LlaveeSecundaria, RutaArchivo, Nuevo.Direccion);
                        ManejadorArboles.InsertaEnArbolSecundario(Aux.Atributos[ArbolS], RutaArchivoArbolSecundario, BitConverter.ToInt32(Nuevo.Informacion[ArbolS], 0), Nuevo.Direccion, RutaArchivo);
                        ActualizaDGVArbolSecundario(Aux);
                    }
                    if (SECUNDARIA != -1)
                    {
                        EliminaIndiceSecundario(Aux, SECUNDARIA, indiceRegistroSeleccionado);
                        AgregaIndiceSecundario(Aux, SECUNDARIA, Nuevo);
                    }
                    for (int i = 0; i < Nuevo.Informacion.Count; i++)
                    {
                        RModificar.Informacion[i] = Nuevo.Informacion[i];
                    }
                    for (int i = 0; i < Aux.Atributos.Count; i++)
                    {
                        if (Aux.Atributos[i].getTipoIndice() == "1")
                        {
                            Cve_Busqueda = true;
                            index = i;
                        }
                    }
                    if (Cve_Busqueda)
                    {
                        OrganizaRegistrosOrdenado(Aux, index);
                    }
                    else
                    {
                        OrganizaRegistros(Aux);
                    }
                    ActualizaDGVRegistros(Aux);
                    ActualizaDGVEntidades();
                }
            }

        }

        private Registro ObtenNuevoRegistro(Entidad Aux)
        {
            Registro NuevoRegistro = new Registro();
            foreach (Atributo a in Aux.Atributos)
            {
                if (a.getTipoAtributo() == "E")
                {
                    Int32 D = Int32.Parse(DGV_AtributosRegistro.CurrentRow.Cells[a.ObtenCadenaNombre()].Value.ToString());
                    byte[] NumeroEnBytes = BitConverter.GetBytes(D);
                    NuevoRegistro.Informacion.Add(NumeroEnBytes);
                }
                else if (a.getTipoAtributo() == "C")
                {
                    char[] Cadena = ConvierteCadena(DGV_AtributosRegistro.CurrentRow.Cells[a.ObtenCadenaNombre()].Value.ToString(), a.Longitud);
                    byte[] CharsEnBytes = Encoding.UTF8.GetBytes(Cadena);
                    NuevoRegistro.Informacion.Add(CharsEnBytes);
                }
                else if (a.getTipoAtributo() == "D")
                {
                    double F = double.Parse(DGV_AtributosRegistro.CurrentRow.Cells[a.ObtenCadenaNombre()].Value.ToString());
                    byte[] DoubleEnBytes = BitConverter.GetBytes(F);
                    NuevoRegistro.Informacion.Add(DoubleEnBytes);
                }
            }
            NuevoRegistro.Direccion = ManejoArchivo.ObtenUltimaDireccion(RutaArchivoDatos, archivo);
            NuevoRegistro.DireccionSiguiente = -1;
            return NuevoRegistro;
        }

        private void BT_EliminarRegistro_Click(object sender, EventArgs e)
        {
            try
            {
                int indiceRegistro = DGV_Registros.CurrentRow.Index;
                Entidad Aux = D.Entidades.ElementAt(indiceEntidadRegistro);
                EliminaRegistro(Aux, indiceRegistro);
            }
            catch (Exception E)
            {
                MessageBox.Show(E.Message);
            }
        }
        private void EliminaRegistro(Entidad Aux, int IndiceRegistro)
        {
            int Primaria = ObtenIndex(Aux, 2);
            int Secundaria = ObtenIndex(Aux, 3);
            int Cve_Busqueda = ObtenIndex(Aux, 1);
            int ArbolP = ObtenIndex(Aux, 4);
            int ArbolS = ObtenIndex(Aux, 5);
            bool IntegridadReferencial;
            IntegridadReferencial = CompruebaSiExisteLaClavePEnTablaDependiente(Aux, BitConverter.ToInt32(Aux.Registros[IndiceRegistro].Informacion[ObtenIndexLlaves(Aux, 'P')], 0));
            if (!IntegridadReferencial)
            {
                if (Primaria != -1 && Aux.Registros.Count > 0)
                {
                    List<Indice> ListaIndices = ManejoArchivo.LeeIndiceBloquePrincipal(RutaArchivoIndicePrimario, archivo, ObtenTipoDatoAtributo(Aux.Atributos[Primaria]), Aux.Atributos[Primaria].Longitud);
                    int IndiceRegistroEnBloqueIndices = ObtenIndexListaIndices(Aux.Registros[IndiceRegistro].Informacion[Primaria], ObtenTipoDatoAtributo(Aux.Atributos[Primaria]), ListaIndices);
                    ListaIndices.RemoveAt(IndiceRegistroEnBloqueIndices);
                    List<Indice> ListaOrdenadaIndices = OrdenaIndices(ListaIndices, ObtenTipoDatoAtributo(Aux.Atributos[Primaria]));
                    EscribeIndices(ListaOrdenadaIndices, ObtenTipoDatoAtributo(Aux.Atributos[Primaria]), RutaArchivoIndicePrimario);
                    ActualizaDGVIndicePrimario(Aux);
                }
                if (Secundaria != -1)
                {
                    EliminaIndiceSecundario(Aux, Secundaria, IndiceRegistro);
                    DGV_CajonIndiceSecundario.Rows.Clear();
                }
                if (ArbolP != -1)
                {
                    ArbolPrimario = new Arbol(ManejoArchivo.ObtenNodos(Aux.Atributos[ArbolP], archivo, RutaArchivoArbolPrimario), Aux.Atributos[ArbolP]);
                    if (ArbolPrimario.Nodos.Count == 1)
                    {
                        ArbolPrimario.Nodos[0].EliminaDatoEnHoja(BitConverter.ToInt32(Aux.Registros[IndiceRegistro].Informacion[ArbolP], 0));
                        ManejoArchivo.EscribeNodo(ArbolPrimario.Nodos[0], archivo, RutaArchivoArbolPrimario);
                        if (ArbolPrimario.Nodos[0].ObtenNumeroLlavesValidas() == 0)
                        {
                            ManejoArchivo.CreaArchivo(RutaArchivoArbolPrimario, archivo);
                            Aux.Atributos[ArbolP].Direccion_Indice = -1;
                            ManejoArchivo.ModificaAtributo(Aux.Atributos[ArbolP], RutaArchivo, archivo);
                        }
                    }
                    else
                    {
                        ManejadorArboles.EliminaEnArbolPrimario(Aux.Atributos[ArbolP], RutaArchivoArbolPrimario, BitConverter.ToInt32(Aux.Registros[IndiceRegistro].Informacion[ArbolP], 0), RutaArchivo);
                    }
                    ActualizaDGVArbolPrimario(Aux);
                }
                if (ArbolS != -1)
                {
                    ManejadorArboles.EliminaEnArbolSecundario(Aux.Atributos[ArbolS], RutaArchivoArbolSecundario, BitConverter.ToInt32(Aux.Registros[IndiceRegistro].Informacion[ArbolS], 0), RutaArchivo, Aux.Registros[IndiceRegistro].Direccion);
                    ActualizaDGVArbolSecundario(Aux);
                }
                Aux.Registros.RemoveAt(IndiceRegistro);

                if (Cve_Busqueda != -1)
                {
                    OrganizaRegistrosOrdenado(Aux, Cve_Busqueda);
                }
                else
                {
                    OrganizaRegistros(Aux);
                }

                if (Aux.Registros.Count == 0)
                {
                    Aux.Direccion_Dato = -1;
                    ManejoArchivo.CreaArchivo(RutaArchivoDatos, archivo);
                    for (int i = 0; i < Aux.Atributos.Count; i++)
                    {
                        Aux.Atributos[i].Direccion_Indice = -1;
                        ManejoArchivo.ModificaAtributo(Aux.Atributos[i], RutaArchivo, archivo);
                    }
                    ManejoArchivo.ModificaEntidad(Aux, RutaArchivo, archivo);
                    ManejoArchivo.ModificaEntidad(Aux, RutaArchivo, archivo);
                    if (Primaria != -1)
                    {
                        ManejoArchivo.EscribeBytesInicialesArchivoIndice(RutaArchivoIndicePrimario, archivo, 0);
                    }
                    if (Secundaria != -1)
                    {
                        ManejoArchivo.CreaArchivo(RutaArchivoIndiceSecundario, archivo);
                        ManejoArchivo.EscribeBytesInicialesArchivoIndice(RutaArchivoIndiceSecundario, archivo, 0);
                    }
                    if (ArbolP != -1)
                    {
                        ManejoArchivo.CreaArchivo(RutaArchivoArbolPrimario, archivo);
                    }
                }
                ActualizaDGVRegistros(Aux);
                ActualizaDGVAtributos(Aux);
                ActualizaDGVEntidades();
                ActualizaDGVBloquePrincipalIndiceSecundario(Aux);
            }
            else
            {
                MessageBox.Show("Existen Registros en tablas Dependientes con el valor de la Llave Primaria de esta Entidad, intenta eliminando primero esos registros.");
            }
        }
        private void EliminaIndiceSecundario(Entidad Aux, int Secundaria, int IndiceRegistro)
        {
            try
            {
                List<Indice> ListaIndicesSecundarios = ManejoArchivo.LeeIndiceBloquePrincipal(RutaArchivoIndiceSecundario, archivo, ObtenTipoDatoAtributo(Aux.Atributos[Secundaria]),
                    Aux.Atributos[Secundaria].Longitud);
                Registro E = Aux.Registros[IndiceRegistro];
                int indexIndice = 0;
                foreach (Indice I in ListaIndicesSecundarios)
                {

                    List<long> Direcciones = ManejoArchivo.LeeCajonIndiceSecundario(RutaArchivoIndiceSecundario, archivo, I.Desplazamiento);
                    for (int j = 0; j < Direcciones.Count; j++)
                    {
                        if (Direcciones[j] == E.Direccion)
                        {
                            Direcciones.RemoveAt(j);
                            ManejoArchivo.EscribeCajonSecundario(RutaArchivoIndiceSecundario, archivo, ListaIndicesSecundarios[indexIndice].Desplazamiento, Direcciones);
                            break;
                        }

                    }
                    indexIndice++;
                }
                ManejoArchivo.EscribeBloqueIndices(RutaArchivoIndiceSecundario, archivo, ListaIndicesSecundarios, 0);
                ActualizaDGVBloquePrincipalIndiceSecundario(Aux);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        private int ObtenIndexListaIndices(byte[] Llave, int TipoDato, List<Indice> ListaIndices)
        {
            int index = -1;
            int j = 0;
            foreach (Indice i in ListaIndices)
            {
                if (TipoDato == 1)
                {
                    int Numero1 = BitConverter.ToInt32(Llave, 0);
                    int Numero2 = BitConverter.ToInt32(i.Llave, 0);
                    if (Numero1 == Numero2)
                    {
                        index = j;
                    }
                }
                else if (TipoDato == 2)
                {
                    string Cad1 = Encoding.UTF8.GetString(Llave);
                    string Cad2 = Encoding.UTF8.GetString(i.Llave);
                    if (Cad1.Equals(Cad2))
                    {
                        index = j;
                    }
                }
                else if (TipoDato == 3)
                {
                    double Numero1 = BitConverter.ToDouble(Llave, 0);
                    double Numero2 = BitConverter.ToDouble(i.Llave, 0);
                    if (Numero1 == Numero2)
                    {
                        index = j;
                    }
                }
                j++;
            }
            return index;
        }
        private int ObtenTipoDatoAtributo(Atributo A)
        {
            int tipo = 0;
            if (A.getTipoAtributo() == "C")
            {
                tipo = 2;
            }
            else if (A.getTipoAtributo() == "E")
            {
                tipo = 1;
            }
            return tipo;
        }

        private void DGV_BloquePrincipalIndiceSecundario_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DGV_CajonIndiceSecundario.Rows.Clear();
            Entidad Aux = D.Entidades.ElementAt(indiceEntidadRegistro);
            int indexS = ObtenIndex(Aux, 3);
            if (indexS != -1 && Aux.Registros.Count > 0)
            {
                indiceBloqueSecundarioSeleccionado = DGV_BloquePrincipalIndiceSecundario.CurrentRow.Index;
                List<Indice> IndicesSecundarios = ManejoArchivo.LeeIndiceBloquePrincipal(RutaArchivoIndiceSecundario, archivo, ObtenTipoDatoAtributo(Aux.Atributos[indexS]),
                    Aux.Atributos[indexS].Longitud);
                List<long> Direcciones = ManejoArchivo.LeeCajonIndiceSecundario(RutaArchivoIndiceSecundario, archivo, IndicesSecundarios[indiceBloqueSecundarioSeleccionado].Desplazamiento);
                int j = 0;
                foreach (long l in Direcciones)
                {
                    DGV_CajonIndiceSecundario.Rows.Add();
                    DGV_CajonIndiceSecundario.Rows[j].Cells["DireccionCSecundario"].Value = l;
                    j++;
                }
            }
        }

        private void ActualizaDGVArbolPrimario(Entidad Aux)
        {
            DGV_ArbolPrimarioBloquePrincipal.Rows.Clear();
            int P = ObtenIndex(Aux, 4);
            int j;
            int z = 0;
            Arbol A = new Arbol(ManejoArchivo.ObtenNodos(Aux.Atributos[P], archivo, RutaArchivoArbolPrimario), Aux.Atributos[P]);
            if (P != -1)
            {
                foreach (Nodo n in A.Nodos)
                {
                    DGV_ArbolPrimarioBloquePrincipal.Rows.Add();
                    j = 0;
                    DGV_ArbolPrimarioBloquePrincipal.Rows[z].Cells[j].Value = n.TipoNodo;
                    DGV_ArbolPrimarioBloquePrincipal.Rows[z].Cells[j + 1].Value = n.DirNodo;
                    j = 2;
                    for (int i = 0; i < n.Llaves.Count; i++)
                    {
                        if (n.DireccionLlaves[i] != -1)
                        {
                            DGV_ArbolPrimarioBloquePrincipal.Rows[z].Cells[j].Value = n.DireccionLlaves[i];
                        }
                        j++;
                        if (n.Llaves[i] != -1)
                        {
                            DGV_ArbolPrimarioBloquePrincipal.Rows[z].Cells[j].Value = n.Llaves[i];
                        }
                        j++;
                    }
                    DGV_ArbolPrimarioBloquePrincipal.Rows[z].Cells[10].Value = n.DireccionLlaves[Arbol.GradoArbol - 1];
                    z++;
                }
            }
        }
        private void ActualizaDGVArbolSecundario(Entidad Aux)
        {
            DGV_ArbolSecundario.Rows.Clear();
            int S = ObtenIndex(Aux, 5);
            int j;
            int z = 0;
            Arbol A = new Arbol(ManejoArchivo.ObtenNodos(Aux.Atributos[S], archivo, RutaArchivoArbolSecundario), Aux.Atributos[S]);
            if (S != -1)
            {
                foreach (Nodo n in A.Nodos)
                {
                    DGV_ArbolSecundario.Rows.Add();
                    j = 0;
                    DGV_ArbolSecundario.Rows[z].Cells[j].Value = n.TipoNodo;
                    DGV_ArbolSecundario.Rows[z].Cells[j + 1].Value = n.DirNodo;
                    j = 2;
                    for (int i = 0; i < n.Llaves.Count; i++)
                    {
                        if (n.DireccionLlaves[i] != -1)
                        {
                            DGV_ArbolSecundario.Rows[z].Cells[j].Value = n.DireccionLlaves[i];
                        }
                        j++;
                        if (n.Llaves[i] != -1)
                        {
                            DGV_ArbolSecundario.Rows[z].Cells[j].Value = n.Llaves[i];
                        }
                        j++;
                    }
                    DGV_ArbolSecundario.Rows[z].Cells[10].Value = n.DireccionLlaves[Arbol.GradoArbol - 1];
                    z++;
                }
            }
        }

        private void ActualizaDGVSArbolSecundario(List<long> Direcciones)
        {
            DGV_ArbolSSecundario.Rows.Clear();
            int j = 0;
            foreach (long l in Direcciones)
            {
                DGV_ArbolSSecundario.Rows.Add();
                DGV_ArbolSSecundario.Rows[j].Cells["Dir"].Value = l;
                j++;
            }
        }



        private void DGV_ArbolSecundario_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            Entidad Aux = D.Entidades.ElementAt(indiceEntidadRegistro);
            int Columna = e.RowIndex;
            int Celda = e.ColumnIndex;
            if (DGV_ArbolSecundario.Rows[Columna].Cells[0].Value + "" == "H")
            {
                long Direccion = Convert.ToInt64(DGV_ArbolSecundario.Rows[Columna].Cells[Celda].Value);
                //MessageBox.Show(Direccion.ToString());
                List<long> Direcciones = ManejoArchivo.LeeCajonIndiceSecundario(RutaArchivoArbolSecundario, archivo, Direccion);
                ActualizaDGVSArbolSecundario(Direcciones);
            }
        }
        private void CB_TipoLlave_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CB_TipoLlave.SelectedIndex == 1)
            {
                FormAtributoFK = new Form2(D.Entidades, D.Entidades.ElementAt(ComboB_EntidadAtributo.SelectedIndex));
                FormAtributoFK.Show();
            }
        }

        private void ComboB_EntidadRelaciones_SelectedIndexChanged(object sender, EventArgs e)
        {
            Entidad E = D.Entidades.ElementAt(ComboB_EntidadRelaciones.SelectedIndex);
            ActualizaDGVRelaciones(E);
        }

        #region Métodos para Relaciones
        /// <summary>
        /// Este método realiza la búsqueda en todas las relaciones de las entidades, para ver si la Entidad tiene relación con alguna tabla.
        /// </summary>
        /// <returns>Regresa Verdadero si tiene Relación con alguna tabla, y Falso en caso contrario.</returns>
        public bool BuscaRelacionTablaPadre(string NombreTabla)
        {
            foreach (Entidad e in D.Entidades)
            {
                foreach (Relacion r in e.Relaciones)
                {
                    if (r.NombreTablaPadre == NombreTabla)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Este método hace la actualización del nombre de la tabla Padre en todas las relaciones que el tenga con las demás entidades.
        /// </summary>
        /// <param name="NombreTabla"></param>
        /// <returns></returns>
        public void ActualizaRelacionTablaPadre(string NombreTabla, string NuevoNombreTabla)
        {
            foreach (Entidad e in D.Entidades)
            {
                foreach (Relacion R in e.Relaciones)
                {
                    if (R.NombreTablaPadre == NombreTabla)
                    {
                        R.NombreTablaPadre = NuevoNombreTabla;
                    }
                }
            }
            ManejoArchivo.GuardaRelaciones(D.Entidades, RutaRelaciones);
        }

        /// <summary>
        /// Este método realiza la búsqueda de la clave ingresada en la tabla "Padre", regresa "true" si la clave existe en la tabla y "false" en caso contrario.
        /// </summary>
        /// <param name="ClaveP"></param>
        /// <param name="E"></param>
        /// <returns></returns>
        public bool BuscaExistenciaClavePrimaria(int ClaveP, Entidad E)
        {
            bool Resultado = false;
            int IndexLlaveP = ObtenIndexLlaves(E, 'P');
            if (IndexLlaveP != -1)
            {
                foreach (Registro R in E.Registros)
                {
                    if (BitConverter.ToInt32(R.Informacion[IndexLlaveP], 0) == ClaveP)
                    {
                        Resultado = true;
                        break;
                    }
                }
            }
            else
            {
                MessageBox.Show("Esta entidad no tiene ningún atributo de tipo Llave Primaria");
            }
            return Resultado;
        }
        /// <summary>
        /// En este método busco el tipo de llave ingresado en los atributos de la Entidad. Regresa -1 si no encuentra el tipo de Llave, en caso contrario regresa el indice de el atributo.
        /// </summary>
        /// <param name="E"></param>
        /// <param name="TipoLlave"></param>
        /// <returns></returns>

        public int ObtenIndexLlaves(Entidad E, char TipoLlave)
        {
            int index = -1;
            for (int i = 0; i < E.Atributos.Count; i++)
            {
                if (E.Atributos[i].Tipo_Llave == TipoLlave)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// En este método busco la Entidad con el nombre ingresado en la lista de Entidades.
        /// </summary>
        /// <param name="Entidades"></param>
        /// <param name="NombreEntidad"></param>
        /// <returns></returns>
        public Entidad EncuentraEntidadPadre(List<Entidad> Entidades, string NombreEntidad)
        {
            Entidad Res = null;

            foreach (Entidad e in Entidades)
            {
                if (NombreEntidad == e.ObtenCadenaNombre())
                {
                    Res = e;
                    break;
                }
            }
            return Res;
        }

        /// <summary>
        /// Este método busca en la Entidad la relación que tiene con la Entidad Padre mediante el atributo Hijo. Regresa la Relación.
        /// </summary>
        /// <param name="EntidadDependiente"></param>
        /// <param name="NombreAtributoEntidadHijo"></param>
        /// <returns></returns>
        public Relacion BuscaRelacion(Entidad EntidadDependiente, string NombreAtributoEntidadHijo)
        {
            Relacion Res = null;
            foreach (Relacion R in EntidadDependiente.Relaciones)
            {
                if (R.NombreAtributoTablaHijo == NombreAtributoEntidadHijo)
                {
                    Res = R;
                    return Res;
                }
            }
            return Res;
        }
        #endregion

        #region Métodos para integridad Referencial
        bool CompruebaIntegridadReferencialInsercionTablaDependiente(Entidad Ent, Registro RegistroInsertar)
        {
            bool Resultado = false;
            for (int i = 0; i < Ent.Atributos.Count; i++)
            {
                if (Ent.Atributos[i].Tipo_Llave == 'S')// Entramos aquí en caso de que encontremos una entidad de tipo Llave Fóranea
                {
                    Relacion R = BuscaRelacion(Ent, Ent.Atributos[i].ObtenCadenaNombre()); // Buscamos la relación que une la tabla padre con la tabla hijo mediante el atributo
                    if (R != null)
                    {
                        Entidad EntidadPadre = EncuentraEntidadPadre(D.Entidades, R.NombreTablaPadre);
                        if (EntidadPadre != null)
                        {
                            string RutaArchivoDatos = RutaCarpeta + "\\" + EntidadPadre.ClaveEnString() + ".dat";
                            ManejoArchivo.LeeDatos(EntidadPadre, RutaArchivoDatos, archivo);
                            Resultado = BuscaExistenciaClavePrimaria(BitConverter.ToInt32(RegistroInsertar.Informacion[i], 0), EntidadPadre);
                            if (!Resultado)
                            {
                                MessageBox.Show("No se encontró ninguna llave primaria con el valor de: " + BitConverter.ToInt32(RegistroInsertar.Informacion[i], 0) +
                                    "\n en la Tabla Padre: " + EntidadPadre.ObtenCadenaNombre());
                                return Resultado;
                            }
                        }
                    }
                    else
                    {
                        Resultado = false;
                        return Resultado;
                    }
                }
            }
            return Resultado;
        }
        List<Entidad> ObtenEntidadesConRelacionTablaPadre(Entidad TablaPadre, List<Entidad> Entidades)
        {
            List<Entidad> EntidadesL = new List<Entidad>();
            foreach (Entidad e in Entidades)
            {
                foreach (Relacion r in e.Relaciones)
                {
                    if (r.NombreTablaPadre == TablaPadre.ObtenCadenaNombre())
                    {
                        EntidadesL.Add(e);
                    }
                }
            }
            return EntidadesL;
        }
        public void ActualizaClavePRegistrosHijos(Entidad EntidadPadre, int ValorRegistroPadre, int ValorAntiguoRegistroPadre)
        {
            List<Entidad> ListaEntidadesHijas = ObtenEntidadesConRelacionTablaPadre(EntidadPadre, D.Entidades);
            MessageBox.Show("Num de Entidades Dependientes: " + ListaEntidadesHijas.Count);
            foreach (Entidad e in ListaEntidadesHijas)
            {
                string RutaEntidad = RutaCarpeta + "\\" + e.ClaveEnString() + ".dat";
                foreach (Relacion r in e.Relaciones)
                {
                    if (r.NombreTablaPadre == EntidadPadre.ObtenCadenaNombre())
                    {
                        int index = ObtenIndiceAtributoString(r.NombreAtributoTablaHijo, e);
                        if (index != -1)
                        {
                            foreach (Registro Reg in e.Registros)
                            {
                                if (BitConverter.ToInt32(Reg.Informacion[index], 0) == ValorAntiguoRegistroPadre)
                                {
                                    Reg.Informacion[index] = BitConverter.GetBytes(ValorRegistroPadre);
                                    ManejoArchivo.ModificaRegistro(Reg, RutaEntidad, archivo);
                                }
                            }
                        }
                    }
                }
            }
        }
        public int ObtenIndiceAtributoString(string NombreAtributo, Entidad Ent)
        {
            int index = -1;
            for (int i = 0; i < Ent.Atributos.Count; i++)
            {
                if (Ent.Atributos[i].ObtenCadenaNombre() == NombreAtributo)
                {
                    index = i;
                    return index;
                }
            }
            return index;
        }

        public bool CompruebaSiExisteLaClavePEnTablaDependiente(Entidad TablaPadre, int ValorAEliminar)
        {
            bool Existe = false;
            List<Entidad> EntidadesDep = ObtenEntidadesConRelacionTablaPadre(TablaPadre, D.Entidades);
            foreach (Entidad e in EntidadesDep)
            {
                foreach (Relacion r in e.Relaciones)
                {
                    if (r.NombreTablaPadre == TablaPadre.ObtenCadenaNombre())
                    {
                        int index = ObtenIndiceAtributoString(r.NombreAtributoTablaHijo, e);
                        if (index != -1)
                        {
                            foreach (Registro Reg in e.Registros)
                            {
                                if (BitConverter.ToInt32(Reg.Informacion[index], 0) == ValorAEliminar)
                                {
                                    Existe = true;
                                }
                            }
                        }
                    }
                }
            }
            return Existe;
        }
        #endregion
        #region Métodos para Consultas SQL

        private void BT_EjecutarConsulta_Click(object sender, EventArgs e)
        {
            String[] Cadena = TextB_Consulta.Text.Split(' ');
            List<string> Cadenas = Cadena.ToList();
            string CadenaAux = "";
            List<string> ColumnasAMostrar = new List<string>();
            List<string> TablasAConsultar = new List<string>();
            string Operador = "";
            string AtributoAEvaluarenWhere = "";
            string AtributoAComparar = "";
            List<string> AtributosACOmpararJoin = new List<string>();
            string ValorNumerico = "";
            int ConsultaWhere = 1; // 1: La más Básica, 2: Ya con where, 3: Con Inner Join
            for (int i = 0; i < Cadenas.Count; i++)//Aquí le quito los espacios a la Tabla
            {
                if (Cadenas[i] == " ")
                {
                    Cadenas.RemoveAt(i);
                    i--;
                }
                CadenaAux += Cadenas[i] + "\n";
            }
            int indexFrom = BuscaIndexPalabra(Cadenas, "From");
            int indexWhere = BuscaIndexPalabra(Cadenas, "Where");
            int indexInner = BuscaIndexPalabra(Cadenas, "Inner");
            int indexJoin = indexInner + 1;

            //MessageBox.Show("Tamaño de la lista: " + Cadenas.Count + "\n" + CadenaAux);
            if (Cadenas[0] == "Select" && indexFrom != -1)
            {
                //En esta sección obtenemos las columnas que vamos a mostrar en el DGV
                for (int i = 1; i < indexFrom; i++)
                {
                    if (indexFrom == 2)
                    {
                        if(Cadenas[i] == "*")
                        {
                            ColumnasAMostrar.Add(Cadenas[i]);
                        }
                        else
                        {
                            if (Cadenas[i].Contains(','))
                            {

                            }
                            ColumnasAMostrar = Cadenas[i].Split(',').ToList();
                        }
                        
                        break;
                    }
                    else
                    {
                        if (Cadenas[i].Contains(","))
                        {
                            ColumnasAMostrar.Add(Cadenas[i].Remove(Cadenas[i].Length - 1, 1));

                        }
                        else
                        {
                            ColumnasAMostrar.Add(Cadenas[i]);
                        }
                    }
                }

                //En esta sección obtenemos la Tabla de la que vamos a sacar la información

                if (indexInner != -1)// En caso de que tengamos un Inner JOIN
                {
                    ConsultaWhere = 3;
                    if(indexInner - indexFrom == 2)
                    {
                        TablasAConsultar.Add(Cadenas[indexFrom + 1]);
                    }
                    if(indexJoin != -1)
                    {
                        TablasAConsultar.Add(Cadenas[indexJoin + 1]);
                    }
                    int indexOn = BuscaIndexPalabra(Cadenas, "On");
                    if(indexOn != -1)
                    {
                        AtributosACOmpararJoin.Add(Cadenas[indexOn + 1]);
                        AtributosACOmpararJoin.Add(Cadenas[indexOn + 3]);
                    }
                }
                else // En caso de que no tengamos un Inner Join
                {
                    if (indexWhere != -1) // En caso de que tengamos un Where
                    {
                        ConsultaWhere = 2;
                        if (indexWhere - indexFrom == 2)
                        {
                            TablasAConsultar.Add(Cadenas[indexWhere - 1]);
                            AtributoAEvaluarenWhere = Cadenas[indexWhere + 1];
                            Operador = Cadenas[indexWhere + 2];
                            ValorNumerico = Cadenas[indexWhere + 3];
                        }

                    }
                    else // En caso de que no tengamos un Where
                    {
                        ConsultaWhere = 1;
                        for (int i = indexFrom + 1; i < Cadenas.Count; i++)
                        {
                            TablasAConsultar.Add(Cadenas[i]);
                        }
                    }
                }
                string CadenaColumnasAMostrar = "";
                string CadenaTablaAConsultar = "";
                foreach (string c in ColumnasAMostrar)
                {
                    CadenaColumnasAMostrar += c + "\n";
                }
                foreach (string c in TablasAConsultar)
                {
                    CadenaTablaAConsultar += c + "\n";
                }
                //MessageBox.Show("Columnas que vamos a mostrar:" + CadenaColumnasAMostrar + "Tamaño: " + CadenaColumnasAMostrar.Length.ToString());
                //MessageBox.Show("Tabla que vamos a consultar: " + CadenaTablaAConsultar);
                //MessageBox.Show("Atributo que vamos a evaluar en la condición Where: " + AtributoAEvaluarenWhere);
                //MessageBox.Show("Operador con el que vamos a evaluar al atributo: " + Operador);
                //MessageBox.Show("Valor con el que vamos a evaluar al atributo: " + ValorNumerico);

                switch (ConsultaWhere)
                {
                    case 1:
                        RegistrosNormales(TablasAConsultar[0], ColumnasAMostrar);
                        break;

                    case 2:
                        List<Registro> Registros = RegistrosWhere(TablasAConsultar[0], AtributoAEvaluarenWhere, ValorNumerico, Operador);
                        if (EncuentraEntidadPadre(D.Entidades, TablasAConsultar[0]) != null)
                        {
                            MuestraRegistrosConFormato(ColumnasAMostrar, Registros, EncuentraEntidadPadre(D.Entidades, TablasAConsultar[0]));
                        }
                        break;
                    case 3:
                        string AtributosAcompararJoin = "";
                        foreach(string c in AtributosACOmpararJoin)
                        {
                            AtributosAcompararJoin += c + "\n";
                        }
                        string TablasJoin = "";
                        foreach(string c in TablasAConsultar)
                        {
                            TablasJoin += c + "\n";
                        }
                        MessageBox.Show("Tablas Inner Join: " + TablasJoin + "\n Atributos a comparar en Inner Join: " + AtributosAcompararJoin +"\n Tablas a mostrar: " + CadenaColumnasAMostrar);
                        List<Registro> RegistrosJ = RegistrosJoin(TablasAConsultar,AtributosACOmpararJoin);
                        MuestraRegistrosJoin(ColumnasAMostrar, RegistrosJ, TablasAConsultar);
                        break;
                }
            }
        }

        public void RegistrosNormales(string NombreTablaAConsultar, List<string> NombreAtributos)
        {
            try
            {
                DGV_ConsultasSQL.Columns.Clear();
                DGV_ConsultasSQL.Rows.Clear();
                Entidad Entidad = EncuentraEntidadPadre(D.Entidades, NombreTablaAConsultar);
                if (Entidad != null)
                {
                    string RutaEntidad = RutaCarpeta + "\\" + Entidad.ClaveEnString() + ".dat";
                    ManejoArchivo.LeeDatos(Entidad, RutaEntidad, archivo);
                    List<int> IndicesAtributosAMostrar = new List<int>();
                    foreach (string c in NombreAtributos)
                    {
                        int indexAtributoMuestra = ObtenIndiceAtributoString(c, Entidad);
                        if (indexAtributoMuestra != -1)
                        {
                            IndicesAtributosAMostrar.Add(indexAtributoMuestra);
                        }
                    }
                    if(NombreAtributos[0] == "*")
                    {
                        for (int i = 0; i < Entidad.Atributos.Count; i++)
                        {
                            IndicesAtributosAMostrar.Add(i);
                        }
                    }
                    List<Atributo> Atributos = new List<Atributo>();
                    foreach (int i in IndicesAtributosAMostrar)
                    {
                        DGV_ConsultasSQL.Columns.Add(Entidad.Atributos[i].ObtenCadenaNombre(), Entidad.Atributos[i].ObtenCadenaNombre());
                        Atributos.Add(Entidad.Atributos[i]);
                    }
                    int z = 0;
                    int j = 0;
                    foreach (Registro R in Entidad.Registros)
                    {
                        DGV_ConsultasSQL.Rows.Add();
                        Registro Aux = new Registro();

                        foreach (int l in IndicesAtributosAMostrar)
                        {
                            Aux.Informacion.Add(R.Informacion[l]);
                        }
                        foreach (byte[] b in Aux.Informacion)
                        {
                            if (Atributos[z].getTipoAtributo() == "E")
                            {
                                int Numero = BitConverter.ToInt32(b, 0);
                                DGV_ConsultasSQL.Rows[j].Cells[Atributos[z].ObtenCadenaNombre()].Value = Numero;
                            }
                            else if (Atributos[z].getTipoAtributo() == "C")
                            {
                                string Cadena = System.Text.Encoding.UTF8.GetString(b);
                                DGV_ConsultasSQL.Rows[j].Cells[Atributos[z].ObtenCadenaNombre()].Value = Cadena;
                            }
                            else if (Atributos[z].getTipoAtributo() == "D")
                            {
                                double Numero = BitConverter.ToDouble(b, 0);
                                DGV_ConsultasSQL.Rows[j].Cells[Atributos[z].ObtenCadenaNombre()].Value = Numero;
                            }
                            z++;
                            if (z >= Atributos.Count)
                            {
                                z = 0;
                            }
                        }
                        j++;
                    }
                }
            }
            catch
            {
            }
        }
        public List<Registro> RegistrosWhere(string NombreTablaAConsultar, string AtributoAEvaluarWhere, string ValorNumerico, string OperadorEvaluacion)
        {
            DGV_ConsultasSQL.Columns.Clear();
            DGV_ConsultasSQL.Rows.Clear();
            List<Registro> Registros = new List<Registro>();
            Entidad Entidad = EncuentraEntidadPadre(D.Entidades, NombreTablaAConsultar);
            if (Entidad != null)
            {
                string RutaEntidad = RutaCarpeta + "\\" + Entidad.ClaveEnString() + ".dat";
                ManejoArchivo.LeeDatos(Entidad, RutaEntidad, archivo);
                int indexAtributo = ObtenIndiceAtributoString(AtributoAEvaluarWhere, Entidad);
                if (indexAtributo != -1)
                {
                    foreach (Registro r in Entidad.Registros)
                    {
                        if (EvaluaCondicion(r, indexAtributo, OperadorEvaluacion, ValorNumerico))
                        {
                            Registros.Add(r);
                        }
                    }
                }
            }
            return Registros;
        }


        public List<Registro> RegistrosJoin(List<string>NombreTablasAConsultar, List<string> AtributosAEvaluarOn)
        {
            List<Registro> RegistrosJoin = new List<Registro>();
            List<Entidad> Entidades = new List<Entidad>();
            foreach(string c in NombreTablasAConsultar)
            {
                Entidad Aux = EncuentraEntidadPadre(D.Entidades, c);
                if(Aux != null)
                {
                    Entidades.Add(Aux);
                }
            }
            if(Entidades.Count == 2)
            {

                Entidad Entidad1 = Entidades[0];
                Entidad Entidad2 = Entidades[1];
                string RutaEnt1 = RutaCarpeta + "\\" + Entidad1.ClaveEnString() + ".dat";
                string RutaEnt2 = RutaCarpeta + "\\" + Entidad2.ClaveEnString() + ".dat";
                ManejoArchivo.LeeDatos(Entidad1, RutaEnt1, archivo);
                ManejoArchivo.LeeDatos(Entidad2, RutaEnt2, archivo);
                string[] Atributo1 = AtributosAEvaluarOn[0].Split('.');
                string[] Atributo2 = AtributosAEvaluarOn[1].Split('.');

                int IndexAtributo1 = ObtenIndiceAtributoString(Atributo1[1], Entidad1);
                int IndexAtributo2 = ObtenIndiceAtributoString(Atributo2[1], Entidad2);
                if (IndexAtributo1 != -1 && IndexAtributo2 != -1) 
                { 
                    foreach(Registro R in Entidad1.Registros)
                    {
                        foreach(Registro R2 in Entidad2.Registros)
                        {
                            if(EvaluaCondicionJoin(R2,IndexAtributo2, BitConverter.ToInt32(R.Informacion[IndexAtributo1], 0)))
                            {
                                RegistrosJoin.Add(CreaRegistroJoin(R, R2));
                            }
                        }
                    }
                }
            }
            return RegistrosJoin;
        }

        private Registro CreaRegistroJoin(Registro R1, Registro R2)
        {
            Registro NuevoRegistro = new Registro();
            foreach(byte[] i in R1.Informacion)
            {
                NuevoRegistro.Informacion.Add(i);
            }
            foreach(byte[] i in R2.Informacion)
            {
                NuevoRegistro.Informacion.Add(i);
            }
            return NuevoRegistro;
        }
        public bool EvaluaCondicionJoin(Registro R, int indexAtributo, int valorNumerico)
        {
            bool Respuesta = false;
            if (BitConverter.ToInt32(R.Informacion[indexAtributo],0) == valorNumerico)
            {
                Respuesta = true;
                return Respuesta;
            }
            return Respuesta;
        }
        public bool EvaluaCondicion(Registro R, int indexAtributo, string OperadorEvaluacion, string ValorNumerico)
        {
            if (OperadorEvaluacion == "!=")
            {
                OperadorEvaluacion = "<>";
            }
            switch (OperadorEvaluacion)
            {
                case "=":
                    if (BitConverter.ToInt32(R.Informacion[indexAtributo], 0) == int.Parse(ValorNumerico))
                    {
                        return true;
                    }
                    break;
                case "<>":
                    if (BitConverter.ToInt32(R.Informacion[indexAtributo], 0) != int.Parse(ValorNumerico))
                    {
                        return true;
                    }
                    break;
                case ">":
                    if (BitConverter.ToInt32(R.Informacion[indexAtributo], 0) > int.Parse(ValorNumerico))
                    {
                        return true;
                    }
                    break;
                case ">=":
                    if (BitConverter.ToInt32(R.Informacion[indexAtributo], 0) >= int.Parse(ValorNumerico))
                    {
                        return true;
                    }
                    break;
                case "<":
                    if (BitConverter.ToInt32(R.Informacion[indexAtributo], 0) < int.Parse(ValorNumerico))
                    {
                        return true;
                    }
                    break;
                case "<=":
                    if (BitConverter.ToInt32(R.Informacion[indexAtributo], 0) <= int.Parse(ValorNumerico))
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }
        /// <summary>
        /// /Método que busca en una lista de Cadenas una palabra. Si encuentra la palabra regresa la posición en la lista, si no, regresa -1
        /// </summary>
        /// <param name="Consulta"></param>
        /// <param name="PalabraABuscart"></param>
        /// <returns></returns>
        private int BuscaIndexPalabra(List<string> Consulta, string PalabraABuscar)
        {
            int index = -1;
            for (int i = 0; i < Consulta.Count; i++)
            {
                if (Consulta[i] == PalabraABuscar)
                {
                    index = i;
                    return index;
                }
            }
            return index;
        }

        private void MuestraRegistrosConFormato(List<string> NombreAtributos,List<Registro> Registros, Entidad Entidad)
        {
            List<int> IndicesAtributosAMostrar = new List<int>();
            foreach (string c in NombreAtributos)
            {
                int indexAtributoMuestra = ObtenIndiceAtributoString(c, Entidad);
                if (indexAtributoMuestra != -1)
                {
                    IndicesAtributosAMostrar.Add(indexAtributoMuestra);
                }
            }
            if (NombreAtributos[0] == "*")
            {
                for(int i = 0; i < Entidad.Atributos.Count; i++)
                {
                    IndicesAtributosAMostrar.Add(i);
                }
            }
            List<Atributo> Atributos = new List<Atributo>();
            foreach (int i in IndicesAtributosAMostrar)
            {
                DGV_ConsultasSQL.Columns.Add(Entidad.Atributos[i].ObtenCadenaNombre(), Entidad.Atributos[i].ObtenCadenaNombre());
                Atributos.Add(Entidad.Atributos[i]);
            }
            //MessageBox.Show("Numero de Atributos: " + Atributos.Count);
            int z = 0;
            int j = 0;
            foreach (Registro R in Registros)
            {
                DGV_ConsultasSQL.Rows.Add();
                Registro Aux = new Registro();

                foreach (int l in IndicesAtributosAMostrar)
                {
                    Aux.Informacion.Add(R.Informacion[l]);
                }
                foreach (byte[] b in Aux.Informacion)
                {
                    if (Atributos[z].getTipoAtributo() == "E")
                    {
                        int Numero = BitConverter.ToInt32(b, 0);
                        DGV_ConsultasSQL.Rows[j].Cells[Atributos[z].ObtenCadenaNombre()].Value = Numero;
                    }
                    else if (Atributos[z].getTipoAtributo() == "C")
                    {
                        string Cadena = System.Text.Encoding.UTF8.GetString(b);
                        DGV_ConsultasSQL.Rows[j].Cells[Atributos[z].ObtenCadenaNombre()].Value = Cadena;
                    }
                    else if (Atributos[z].getTipoAtributo() == "D")
                    {
                        double Numero = BitConverter.ToDouble(b, 0);
                        DGV_ConsultasSQL.Rows[j].Cells[Atributos[z].ObtenCadenaNombre()].Value = Numero;
                    }
                    z++;
                    if (z >= Atributos.Count)
                    {
                        z = 0;
                    }
                }
                j++;
            }
        }

        private void MuestraRegistrosJoin(List<string> NombreAtributosAMostrar, List<Registro> RegistrosJoin, List<string> TablasJoin)
        {
            try
            {
                DGV_ConsultasSQL.Columns.Clear();
                DGV_ConsultasSQL.Rows.Clear();
                Entidad Entidad1 = EncuentraEntidadPadre(D.Entidades, TablasJoin[0]);
                Entidad Entidad2 = EncuentraEntidadPadre(D.Entidades, TablasJoin[1]);
                if (Entidad1 != null && Entidad2 != null)
                {

                    List<int> AtributosEntidad1 = new List<int>();
                    List<int> AtributosEntidad2 = new List<int>();

                    foreach (string c in NombreAtributosAMostrar)
                    {
                        int IndexEnt1 = ObtenIndiceAtributoString(c, Entidad1);
                        int IndexEnt2 = ObtenIndiceAtributoString(c, Entidad2);
                        if (IndexEnt1 != -1)
                        {
                            // MessageBox.Show("Indice Entidad1: " + IndexEnt1);
                            AtributosEntidad1.Add(IndexEnt1);
                        }
                        else if (IndexEnt2 != -1)
                        {
                            //MessageBox.Show("Indice Entidad2: " + IndexEnt2);
                            AtributosEntidad2.Add(IndexEnt2);
                        }
                    }
                    //MessageBox.Show("Num Atributos Entidad1: " + AtributosEntidad1.Count);
                    //MessageBox.Show("NumAtributos Entidad 2: " + AtributosEntidad2.Count);
                    if (NombreAtributosAMostrar[0] == "*")
                    {
                        for (int i = 0; i < Entidad1.Atributos.Count; i++)
                        {
                            AtributosEntidad1.Add(i);
                        }
                        for (int i = 0; i < Entidad2.Atributos.Count; i++)
                        {
                            AtributosEntidad2.Add(i);
                        }
                    }
                    List<Atributo> Atributos = new List<Atributo>();
                    foreach (int i in AtributosEntidad1)
                    {
                        //DGV_ConsultasSQL.Columns.Add(Entidad1.Atributos[i].ObtenCadenaNombre(), Entidad1.Atributos[i].ObtenCadenaNombre());
                        Atributos.Add(Entidad1.Atributos[i]);
                    }
                    foreach (int i in AtributosEntidad2)
                    {
                        //DGV_ConsultasSQL.Columns.Add(Entidad2.Atributos[i].ObtenCadenaNombre(), Entidad2.Atributos[i].ObtenCadenaNombre());
                        Atributos.Add(Entidad2.Atributos[i]);
                    }
                    //MessageBox.Show("Numero de atributos:" + Atributos.Count.ToString());
                    foreach (Atributo a in Atributos)
                    {
                        DGV_ConsultasSQL.Columns.Add(a.ObtenCadenaNombre(), a.ObtenCadenaNombre());
                    }
                    //MessageBox.Show("PAUSA");
                    int z = 0;
                    int j = 0;
                    foreach (Registro R in RegistrosJoin)
                    {
                        if (Atributos.Count > 0)
                        {
                            //MessageBox.Show("Cantidad de Información en Registros Join: " + R.Informacion.Count);
                            DGV_ConsultasSQL.Rows.Add();
                            Registro Aux = new Registro();

                            foreach (int l in AtributosEntidad1)
                            {
                                Aux.Informacion.Add(R.Informacion[l]);
                            }
                            foreach (int l in AtributosEntidad2)
                            {
                                Aux.Informacion.Add(R.Informacion[l + Entidad1.Atributos.Count]);
                            }
                            //MessageBox.Show("Número de valores en el registro: " + Aux.Informacion.Count.ToString());
                            foreach (byte[] b in Aux.Informacion)
                            {
                                if (Atributos[z].getTipoAtributo() == "E")
                                {
                                    int Numero = BitConverter.ToInt32(b, 0);
                                    DGV_ConsultasSQL.Rows[j].Cells[z].Value = Numero;
                                }
                                else if (Atributos[z].getTipoAtributo() == "C")
                                {
                                    string Cadena = System.Text.Encoding.UTF8.GetString(b);
                                    DGV_ConsultasSQL.Rows[j].Cells[z].Value = Cadena;
                                }
                                else if (Atributos[z].getTipoAtributo() == "D")
                                {
                                    double Numero = BitConverter.ToDouble(b, 0);
                                    DGV_ConsultasSQL.Rows[j].Cells[z].Value = Numero;
                                }
                                z++;
                                if (z >= Atributos.Count)
                                {
                                    z = 0;
                                }
                            }
                            j++;
                        }
                    }
                }
            }
            catch
            {

            } 
        }
        #endregion

        private void BT_LimpiarConsultaSQL_Click(object sender, EventArgs e)
        {

            TextB_Consulta.Text = "";
            if(DGV_ConsultasSQL.Columns.Count != 0)
            {
                DGV_ConsultasSQL.Columns.Clear();
                DGV_ConsultasSQL.Rows.Clear();
            }  
        }
    }
}
