using System;

public class Signal_processing
{
    public static double[] Zero_average_corr(double [] w, int index)
    {
        double[] angle,Av;
        angle = new double[index];
        Av = new double[index];
        angle[0] = 0;
        for (int i = 1; i < index; i++) angle[i] = Simple_Integration(angle[i-1],w[i],0.01);
        for (int i = 0; i < index; i++) Av[i] = Average500(angle,i);
        for (int i = 0; i < index; i++) angle[i] = angle[i] - Av[i];
        return angle;
    }
    private static double Simple_Integration(double A, double B, double dt)
    {        
        return A+B*dt;
    }
    public static float Average50(float[] A, int index)
    {
        float Summ = 0;
        if (index < 25)
        {
            for (int i = 0; i < index + 25; i++)
            {
                Summ += A[i];
            }
            return Summ / (index + 26);
        }
        if (index >= 25 && A.Length - index > 25)
        {
            for (int i = index - 25; i < index + 25; i++)
            {
                Summ += A[i];
            }
            return Summ / (index + 50);
        }
        if (A.Length - index < 25)
        {
            for (int i = index - 25; i < A.Length; i++)
            {
                Summ += A[i];
            }
            return Summ / (A.Length - index + 26);
        }
        return 1;
    }
    private static double Average500(double[] A, int index)
    {
        double Summ = 0;
        if (index < 250)
        {
            for (int i = 0; i < index + 199; i++)
            {
                Summ += A[i];
            }
            return Summ / (index + 198);
        }
        if (index>=250 && A.Length-index > 250)
        {
            for (int i = index - 250; i < index + 250; i++)
            {
                Summ += A[i];
            }
            return Summ / 500;
        }
        if (A.Length - index < 251)
        {
            for (int i = index - 250; i < A.Length; i++)
            {
                Summ += A[i];
            }
            return Summ / (A.Length - index + 249);
        }
        return 1;
    }

    public static Tuple<float[], int, int> find_stroke(float[] source, double zero = 0)
    {
        float[] result;
        bool found = false;
        int start_ind = 0;
        int zero_ind = 0;
        int finish_ind = 0;
        int last_start_ind = 0;
        for (int i = 1; i < source.Length; i++)
        {
            if (!found)
            {
                if ((source[i] >= zero + 0.1) && (source[i] > source[i - 1]))
                {
                    found = true;
                    finish_ind = i;
                    //if ((start_ind != finish_ind)&&(start_ind != 0))
                    //    break;
                    last_start_ind = start_ind;
                    start_ind = i;
                }
            }
            else
            {
                if ((source[i] < zero) && (source[i - 1] >= zero))
                {
                    zero_ind = i;
                    found = false;
                }
            }
        } // for (int i = 1; i < source.Length; i++)
        result = new float[finish_ind - last_start_ind + 1];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = source[last_start_ind + i];
        }
        return new Tuple<float[], int, int>(result, last_start_ind, finish_ind);
    }

    public static float low_pass_filter(float[] A, int index)
    {
        float[] Coefs = {-0.012473190450515F,
                 -0.008369834604751F,
                 -0.003068831331608F,
                  0.003324567275908F,
                  0.010652516769684F,
                  0.018709785675773F,
                  0.027251331401229F,
                  0.036001924510809F,
                  0.044667391854177F,
                  0.052946962389064F,
                  0.060546138291253F,
                  0.067189481467086F,
                  0.072632703791868F,
                  0.076673478585970F,
                  0.079160449678505F,
                                  0F,
                  0.079160449678505F,
                  0.076673478585970F,
                  0.072632703791868F,
                  0.067189481467086F,
                  0.060546138291253F,
                  0.052946962389064F,
                  0.044667391854177F,
                  0.036001924510809F,
                  0.027251331401229F,
                  0.018709785675773F,
                  0.010652516769684F,
                  0.003324567275908F,
                 -0.003068831331608F,
                 -0.008369834604751F,
                 -0.012473190450515F};
        float Summ = 0;
        if (index < 15)
        {
            return A[index];
        }
        if (index >= 15 && A.Length - index > 15)
        {
            for (int i = index - 15; i < index + 15; i++)
            {
                Summ += A[i]*Coefs[i+15-index];
            }
            return Summ;
        }
        if (A.Length - index <= 15)
        {
            return A[index];
        }
        return 1;
    }

    public static int[] stroke_find_2(float[] source, double zero, int delay)
    {
        int[] result = new int[2];
        bool found = false;
        int old = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (!found)
            {
                if ((source[i] <= zero))
                {
                    found = true;
                    old = result[0];
                    result[0] = i;
                    
                }
            }
            else
            {
                if ((source[i] <= zero)&&((i-result[0] >= delay)))
                {
                    result[1] = i;
                    found = false;
                }
            }
        }
        if (found)
        {
            result[0] = old;
        }

        return result;
    }

    public static float[] slide_aver5(float[] source)
    {
        //float result = 0;
        //int len = (source.Length);
        //for (int i = 0; i < len; i++)
        //    result += source[i] / len;
        //return result;
        //float[] filtred = new float[source.Length];
        //filtred[0] = source[0] / 2 + source[1] / 2;
        //filtred[18] = source[18] / 2 + source[17] / 2;
        //for (int i = 1; i < 18; i++)
        //    filtred[i] = source[i - 1] / 3 + source[i] / 3 + source[i+1] / 3;
        float[] filtred = new float[source.Length];
        filtred[0] = source[0] / 3 + source[1] / 3 + source[2] / 3;
        filtred[1] = source[0]/4 + source[1] / 4 + source[2] / 4 + source[3] / 4;
        filtred[18] = source[18] / 3 + source[17] / 3 +source[16] / 3;
        filtred[17] = source[18] / 4 + source[17] / 4 + source[16] / 4 + source[15] / 4;
        for (int i = 2; i < 17; i++)
            filtred[i] = source[i - 2] / 5 + source[i - 1] / 5 + source[i] / 5 +source[i + 1] / 5 + source[i + 2] / 5;
        return filtred;
    }

    public static float[] slide_aver3(float[] source)
    {

        float[] filtred = new float[source.Length];
        filtred[0] = source[0] / 2 + source[1] / 2;
        filtred[18] = source[18] / 2 + source[17] / 2;
        for (int i = 1; i < 18; i++)
            filtred[i] = source[i - 1] / 3 + source[i] / 3 + source[i + 1] / 3;
     
        return filtred;
    }
}
