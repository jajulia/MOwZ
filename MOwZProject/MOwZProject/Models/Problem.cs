﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MOwZProject.Models
{
    public class Problem
    {
        [DisplayName("Rozważane stany")]
        [Required(ErrorMessage="Konieczne jest podanie stanów!")]
        public List<State> States { get; set; }
        
        [DisplayName("Wielkość parlamentu")]
        [Required(ErrorMessage = "Wielkość parlamentu jest polem obowiązkowym")]
        [Range(0, int.MaxValue, ErrorMessage = "Wpisz liczbę z odpowiedniego przedziału")]
        public int ParlamentSize { get; set; }
        

        //Przydziały w koljenych iteracjach
        public List<int> Iterations;

        [DisplayName("Wyświetl szczegóły przetwarzania")]
        public bool details { get; set; }

        public List<Step> Steps;

        public Problem()
        {
            States = new List<State>();
            Iterations = new List<int>();
        }







        /////////////////////////////////////////////////////////////////////////
        /////// WSYZTSKO PONIZEJ DO ZMIANY ZEBY DZIALALO NA OBIEKCIE ////////////
        /////////////////////////////////////////////////////////////////////////



        /// <summary>
        /// Metoda wykonuje algorytm Stilla.
        /// </summary>
        /// <param name="all">Liczba rozważanych stanów.</param>
        /// <param name="size">Liczba miejsc do przydziału (rozmair parlamentu).</param>
        /// <param name="p">Wektor liczności poszczególnych stanów.</param>
        /// <returns>Lista kolejnych przydziałów miejsc.</returns>
        public void getStillResult()
        {
            
            int[] a = new int[this.States.Count()];

            for (int i = 0; i < this.States.Count() - 1; i++)
            {
                a[i] = 0;
            }


            SortedList<int, double> list = new SortedList<int, double>();
            int temp;

            if (this.details)
            {
                this.Steps = new List<Step>();
            }

            for (int hi = 1; hi <= this.ParlamentSize; hi++)
            {

                for (int i = 0; i < this.States.Count(); i++)
                {
                    list.Add(i, this.States.ElementAt(i).Size / hill(a[i]));
                }

                /////////////////var l = list.OrderByDescending(x => x.Value);

                //z obiektow do listy icznosci
                int[] p = new int[this.States.Count()];

                for(int no =0; no<this.States.Count(); no++)
                {
                    p[no] = this.States.ElementAt(no).Size;
                }

                try
                {
                    temp = still(p, list.OrderByDescending(x => x.Value), a, hi);
                    this.Iterations.Add(temp); //dla danej iteracji komu przydzielono
                    //zwiekszenie licbzy mandatow
                    this.States.Find(s => s.id == temp).Mandats++;
                    a[temp]++;

                    if (a[temp] > p[temp])//Stan nie może mieć więcej miejsc w parlamencie niż obywateli.
                    {
                        throw new Exception(String.Format("Nie da się przydzielić stanowi więcej miejsc w parlamencie, bo stan ma za mało obywateli"));
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    throw new Exception(String.Format("Nie da się przydzielić {0} miejsca w parlamencie", hi));
                    
                }

                list.Clear();
            }

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="list"></param>
        /// <param name="a"></param>
        /// <param name="hi"></param>
        /// <returns>Numer stanu, któremu przydzielono miejsce lub -1.</returns>
        private int still(int[] p, IOrderedEnumerable<KeyValuePair<int, double>> list, int[] a, int hi)
        {
            for (int i = 0; i < list.Count(); i++)
            {
                if (details)
                {
                    this.Steps.Add(new Step());
                    this.Steps.Last().Element = this.States.Find(s => s.id == list.ElementAt(i).Key).Name;
                }
                if (spelniaGornaKwote(p[list.ElementAt(i).Key], p.Sum(), hi, a[list.ElementAt(i).Key]) &&
                    spelniaDolnaKwote(hi, list.ElementAt(i).Key, p.Sum(), list, p, a))
                {
                    if (details && this.Steps.Last().DolnaKwota == null)
                    {
                        this.Steps.Last().DolnaKwota = "Niespełniono testu górnej kwoty";
                    }
                    return list.ElementAt(i).Key;
                }
            }
            return -1;
        }



        /// <summary>
        /// Metoda przeprowadza test dolnej kwoty.
        /// </summary>
        /// <param name="h"></param>
        /// <param name="index"></param>
        /// <param name="suma"></param>
        /// <param name="list"></param>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <returns>Informacja, czy dany stan spełnia test dolnej kwoty.</returns>
        private bool spelniaDolnaKwote(int h, int index, int suma, IOrderedEnumerable<KeyValuePair<int, double>> list, int[] p, int[] a)
        {
            int pi = p[index];
            int n = p.Length;

            double tmp = Math.Ceiling((double)suma / pi);
            int hb = tmp < n ? Convert.ToInt32(tmp) : n + 1; //+1, bo potem sprawdzamy do <hb, ale tylko w przypadku gdy wartość jest niezmieniona, tj. wartość mianownika/licznik


            int[] s = new int[n];

            for (int hi = h; hi < hb; hi++)
            {
                for (int k = 0; k < n; k++)
                {
                    if (list.ElementAt(k).Value == index)
                    {
                        s[k] = a[list.ElementAt(k).Key] + 1;
                    }
                    else
                    {
                        s[k] = Math.Max(a[list.ElementAt(k).Key], dolnaKwota(p[list.ElementAt(k).Key], suma, hi));
                    }
                }

                if (this.details)
                {
                    this.Steps.Last().DolnaKwota = "(" + s.Sum().ToString() + " ≤ " + hi.ToString() + ") AND (" + s.Sum().ToString() + " < " + hb.ToString() + ")";
                    this.Steps.Last().SpelniaTestDolnejKwoty = (s.Sum() <= hi && s.Sum() < hb);
                }

                if (s.Sum() > hi || s.Sum() >= hb)
                {
                    return false;
                }
            }
            if (this.details && this.Steps.Last().DolnaKwota == null)
            {
                this.Steps.Last().DolnaKwota = "Sepełniony, ale nie wiem dlaczego";
                this.Steps.Last().SpelniaTestDolnejKwoty = true;
            }
            return true;
        }



        /// <summary>
        /// Metoda oblicza wartość dolnej kwoty.
        /// </summary>
        /// <param name="pi"></param>
        /// <param name="Epi"></param>
        /// <param name="ha"></param>
        /// <returns>Wartość dolnej kwoty.</returns>
        private static int dolnaKwota(double pi, double Epi, double ha)
        {
            return Convert.ToInt32(Math.Floor(pi * ha / Epi));
        }




        /// <summary>
        /// Metoda przeprowadza test górnej kwoty dla stanu.
        /// </summary>
        /// <param name="pi">Liczność stanu</param>
        /// <param name="Epi"></param>
        /// <param name="ha"></param>
        /// <param name="ai">Miejsca przydzielone dla tego stanu.</param>
        /// <returns>Informacja, czy dany stan spełnia test górnej kwoty.</returns>
        private bool spelniaGornaKwote(double pi, double Epi, int ha, int ai)
        {
            if (this.details)
            {
                this.Steps.Last().GornaKwota = "⌈ (" + pi.ToString() + " * " + ha.ToString() + ") / " + Epi.ToString() + " ⌉ >= " + ai.ToString();
                this.Steps.Last().SpelniaTestGornejKwoty = Math.Ceiling((pi * ha) / Epi) >= ai;
            }
            return Math.Ceiling((pi * ha) / Epi) >= ai;
        }


        /// <summary>
        /// Metoda oblicza wartość dla kryterium Hilla.
        /// </summary>
        /// <param name="a">Przydzielone miejsca dla analizowanego stanu.</param>
        /// <returns>Wartość obliczoną dla kryterium.</returns>
        private static double hill(int a)
        {
            int b = a + 1;
            return Math.Sqrt(b * (b + 1));
        }

    }
}