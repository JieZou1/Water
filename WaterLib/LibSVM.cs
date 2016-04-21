using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace WaterLib
{
    public struct LibSVMNode { public int index; public double value;    }

    public enum LibSVMType
    {
        C_SVC = 0,      //C-SVM classification
        NU_SVC,         //nu-SVM classification
        ONE_CLASS,      //one-class-SVM
        EPSILON_SVR,    //epsilon-SVM regression
        NU_SVR,         //nu-SVM regression

    }

    /// <summary>
    /// SVM is a wrapper on top of LibSVM C++ library (http://www.csie.ntu.edu.tw/~cjlin/libsvm/), 
    /// This class has to be used along with libsvm.dll, a native C++ DLL.
    /// </summary>
    public class LibSVM : IDisposable
    {
        public const string version = "3.0";

        /// <summary>
        /// A pointer to the svm model loaded (and malloc'ed) by native C++ DLL.
        /// </summary>
        private IntPtr svm_model = IntPtr.Zero;

        /// <summary>
        /// The class labels of this SVM classification
        /// </summary>
        public int[] Labels { get { return labels; } }
        private int[] labels = null;

        /// <summary>
        /// The type of this SVM classification/regression
        /// </summary>
        public LibSVMType Type { get { return type; } }
        private LibSVMType type;

        /// <summary>
        /// Number of Support Vectors
        /// </summary>
        public int NrSV { get { return nrSV; } }
        private int nrSV;

        /// <summary>
        /// Number of features
        /// </summary>
        public int NrFeature { get { return nrFeature; } }
        private int nrFeature;

        #region IDisposable Members

        public unsafe void Dispose()
        {
            if (svm_model != IntPtr.Zero) 
            {
                //svm_free_and_destroy_model(svm_model);
                svm_destroy_model(svm_model);
                svm_model = IntPtr.Zero; 
            }
        }

        #endregion

        /// <summary>
        /// Destructor, to free svm_model, if it has been loaded.
        /// </summary>
        ~LibSVM() { this.Dispose(); }

        /// <summary>
        /// Convert the SVM Linear model to single vector representation
        /// </summary>
        public unsafe float[] ToSingleVector()
        {
            LibSVMNode[][] support_vectors = new LibSVMNode[nrSV][];
            for (int i = 0; i < nrSV; i++)
            {
                LibSVMNode[] sv = new LibSVMNode[nrFeature];
                fixed (LibSVMNode* sv_p = sv)
                {
                    svm_get_sv(svm_model, i, new IntPtr(sv_p));
                }
                support_vectors[i] = sv;
            }

            double rho = svm_get_rho(svm_model);

            double[] coef = new double[nrSV];
            fixed (double* coef_p = coef)
            {
                svm_get_coef(svm_model, new IntPtr(coef_p));
            }

            float[] single_vector = new float[nrFeature + 1]; int index; double value;
            for (int i = 0; i < nrSV; i++)
            {
                for (int j = 0; j < support_vectors[i].Length; j++)
                {
                    index = support_vectors[i][j].index;
                    if (index == -1) break;
                    value = support_vectors[i][j].value;

                    single_vector[index - 1] += (float)(value * coef[i]);
                }
            }
            single_vector[nrFeature] = -(float)rho;

            return single_vector;
        }

        /// <summary>
        /// Load svm model from a file and store it in svm_model.
        /// It also sets the Type and Labels properties.
        /// ATTENTION: The native C++ DLL is also responsible for releasing this memory.
        /// Basically, here we just want to maintain one svm_model in one SVM instance.
        /// So the previous svm_model will be freed when a new svm_model is loaded.
        /// Of course, destructor will free the svm_model to prevent memory leak.
        /// </summary>
        /// <param name="model_file">The file path of the svm model.</param>
        public unsafe void LoadModel(string model_file)
        {
            this.Dispose();
            //fixed (char* model_file_p = model_file)
            //{
            //    svm_model = svm_load_model(new IntPtr(model_file_p));
            //}
            svm_model = svm_load_model(model_file);
            if (svm_model == IntPtr.Zero) throw new Exception("Load SVM Model Unsuccessful");

            type = (LibSVMType)svm_get_svm_type(svm_model);

            if (type == LibSVMType.C_SVC || type == LibSVMType.NU_SVC)
            {
                labels = new int[svm_get_nr_class(svm_model)];
                fixed (int* labels_p = labels)
                {
                    svm_get_labels(svm_model, new IntPtr(labels_p));
                }
            }

            nrSV = svm_get_nr_sv(svm_model);
            nrFeature = svm_get_nr_feature(svm_model);
        }

        #region svm_predict
        /// <summary>
        /// SVM classification or regression of a sample. If want to get estimated probabilities, use PredictProb().
        /// </summary>
        /// <param name="x">The sample feature vector, following LibSVM format. Each feature has an index and a feature value. Use index=-1 to indicate the end of x.</param>
        /// <returns>The returned SVM classification or regression result, depending on the svm model.</returns>
        public unsafe double Predict(LibSVMNode[] x)
        {
            double svm_result;
            fixed (LibSVMNode* x_p = x) //use fixed to pin the memory down.
            {
                svm_result = svm_predict(svm_model, new IntPtr(x_p));
            }
            return svm_result;
        }

        /// <summary>
        /// SVM classification or regression of a sample. If want to get estimated probabilities, use PredictProb().
        /// </summary>
        /// <param name="x">The sample feature vector, use NaN for missing features.</param>
        /// <returns>The returned SVM classification or regression result, depending on the svm model.</returns>
        public double Predict(List<double> x)
        {
            return Predict(ToLibSVMFormat(x));
        }

        /// <summary>
        /// SVM classification or regression of a sample. If want to get estimated probabilities, use PredictProb().
        /// </summary>
        /// <param name="x">The sample feature array, use NaN for missing features.</param>
        /// <returns>The returned SVM classification or regression result, depending on the svm model.</returns>
        public double Predict(double[] x)
        {
            return Predict(ToLibSVMFormat(x));
        }

        /// <summary>
        /// SVM classification or regression of a set of samples. If want to get estimated probabilities, use PredictProb().
        /// </summary>
        /// <param name="xs">The sample feature vector, following LibSVM format. Each feature has an index and a feature value. Use index=-1 to indicate the end of x.</param>
        /// <returns>The returned SVM classification or regression results, depending on the svm model.</returns>
        public double[] Predict(LibSVMNode[][] xs)
        {
            int n = xs.GetLength(0); double[] svm_results = new double[n];
            for (int i = 0; i < n; i++)
            {
                svm_results[i] = Predict(xs[i]);
            }
            return svm_results;
        }

        /// <summary>
        /// SVM classification or regression of a set of samples. If want to get estimated probabilities, use PredictProb().
        /// </summary>
        /// <param name="xs">The sample feature vectors, use NaN for missing features.</param>
        /// <returns>The returned SVM classification or regression results, depending on the svm model.</returns>
        public double[] Predict(List<List<double>> xs)
        {
            int n = xs.Count; double[] svm_results = new double[n];
            for (int i = 0; i < n; i++)
            {
                svm_results[i] = Predict(xs[i]);
            }
            return svm_results;
        }

        /// <summary>
        /// SVM classification or regression of a set of samples. If want to get estimated probabilities, use PredictProb().
        /// </summary>
        /// <param name="xs">The sample feature vectors, use NaN for missing features.</param>
        /// <returns>The returned SVM classification or regression results, depending on the svm model.</returns>
        public double[] Predict(double[][] xs)
        {
            int n = xs.GetLength(0); double[] svm_results = new double[n];
            for (int i = 0; i < n; i++)
            {
                svm_results[i] = Predict(xs[i]);
            }
            return svm_results;
        }

        #endregion

        #region svm_predict_probability

        /// <summary>
        /// SVM classification of a sample. If want to predict the label only, call Predict().
        /// Read the comments to para x, make sure you understand how to represent a sample in SVMNode[] type.
        /// </summary>
        /// <param name="x">The sample feature vector, following LibSVM format. Each feature has an index and a feature value. Use index=-1 to indicate the end of x.</param>
        /// <param name="probs">The returned estimated classification probabilities.
        /// ATTENTION: The order of the probs are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which is the order of probs.</param>
        /// <returns>The returned classification label.</returns>
        public unsafe double PredictProb(LibSVMNode[] x, out double[] probs)
        {
            probs = new double[Labels.Length]; double label;

            fixed (LibSVMNode* x_p = x) //use fixed to pin the memory down.
            {
                fixed (double* probs_p = probs)
                {
                    label = svm_predict_probability(svm_model, new IntPtr(x_p), new IntPtr(probs_p));
                }
            }
            return label;
        }

        /// <summary>
        /// SVM classification of a sample. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="x">The sample feature vector, use NaN for missing features.</param>
        /// <param name="probs">The returned estimated classification probabilities.
        /// ATTENTION: The order of the probs are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which is the order of probs.</param>
        /// <returns>The returned classification label.</returns>
        public double PredictProb(List<double> x, out double[] probs)
        {
            return PredictProb(ToLibSVMFormat(x), out probs);
        }

        /// <summary>
        /// SVM classification of a sample. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="x">The sample feature array, use NaN for missing features.</param>
        /// <param name="probs">The returned estimated classification probabilities.
        /// ATTENTION: The order of the probs are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which is the order of probs.</param>
        /// <returns>The returned classification label.</returns>
        public double PredictProb(double[] x, out double[] probs)
        {
            return PredictProb(ToLibSVMFormat(x), out probs);
        }

        public double PredictProb(float[] x, out double[] probs)
        {
            return PredictProb(ToLibSVMFormat(x), out probs);
        }

        /// <summary>
        /// SVM classification of a set of samples. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="xs">The sample feature vector, use NaN for missing features.</param>
        /// <param name="probs">The returned estimated classification probabilities.
        /// ATTENTION: The order of the probs are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which is the order of probs.</param>
        /// <returns>The returned classification labels.</returns>
        public double[] PredictProb(LibSVMNode[][] xs, out double[][] probs)
        {
            int n = xs.GetLength(0); double[] labels = new double[n]; probs = new double[n][];
            for (int i = 0; i < n; i++)
            {
                labels[i] = PredictProb(xs[i], out probs[i]);
            }
            return labels;
        }

        /// <summary>
        /// SVM classification of a set of samples. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="xs">The sample feature vector, use NaN for missing features.</param>
        /// <param name="probs">The returned estimated classification probabilities.
        /// ATTENTION: The order of the probs are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which is the order of probs.</param>
        /// <returns>The returned classification label.</returns>
        public double[] PredictProb(List<List<double>> xs, out double[][] probs)
        {
            int n = xs.Count; double[] labels = new double[n]; probs = new double[n][];
            for (int i = 0; i < n; i++)
            {
                labels[i] = PredictProb(xs[i], out probs[i]);
            }
            return labels;
        }

        /// <summary>
        /// SVM classification of a set of samples. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="xs">The sample feature arrays, use NaN for missing features.</param>
        /// <param name="probs">The returned estimated classification probabilities.
        /// ATTENTION: The order of the probs are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which is the order of probs.</param>
        /// <returns>The returned classification label.</returns>
        public double[] PredictProb(double[][] xs, out double[][] probs)
        {
            int n = xs.GetLength(0); double[] labels = new double[n]; probs = new double[n][];
            for (int i = 0; i < n; i++)
            {
                labels[i] = PredictProb(xs[i], out probs[i]);
            }
            return labels;
        }

        #endregion

        #region svm_predict_values

        /// <summary>
        /// Call svm_predict_values function. 
        /// SVM classification of a sample. If want to predict the label only, call Predict().
        /// Read the comments to para x, make sure you understand how to represent a sample in SVMNode[] type.
        /// </summary>
        /// <param name="x">The sample feature vector, following LibSVM format. Each feature has an index and a feature value. Use index=-1 to indicate the end of x.</param>
        /// <param name="dec_values">The returned decision values.
        /// ATTENTION: The order of the dec_values are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which decides the order of dec_values.</param>
        /// <returns>The returned classification label.</returns>
        public unsafe double PredictValues(LibSVMNode[] x, out double[] dec_values)
        {
            dec_values = new double[labels.Length * (labels.Length - 1) / 2]; double label;

            fixed (LibSVMNode* x_p = x) //use fixed to pin the memory down.
            {
                fixed (double* dec_values_p = dec_values)
                {
                    label = svm_predict_values(svm_model, new IntPtr(x_p), new IntPtr(dec_values_p));
                }
            }
            return label;
        }

        /// <summary>
        /// Call svm_predict_values function. 
        /// SVM classification of a sample. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="x">The sample feature vector, use NaN for missing features.</param>
        /// <param name="dec_values">The returned decision values.
        /// ATTENTION: The order of the dec_values are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which decides the order of dec_values.</param>
        /// <returns>The returned classification label.</returns>
        public double PredictValues(List<double> x, out double[] dec_values)
        {
            return PredictValues(ToLibSVMFormat(x), out dec_values);
        }

        /// <summary>
        /// Call svm_predict_values function. 
        /// SVM classification of a sample. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="x">The sample feature array, use NaN for missing features.</param>
        /// <param name="dec_values">The returned decision values.
        /// ATTENTION: The order of the dec_values are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which decides the order of dec_values.</param>
        /// <returns>The returned classification label.</returns>
        public double PredictValues(double[] x, out double[] dec_values)
        {
            return PredictValues(ToLibSVMFormat(x), out dec_values);
        }

        /// <summary>
        /// Call svm_predict_values function. 
        /// SVM classification of a set of samples. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="xs">The sample feature vector, use NaN for missing features.</param>
        /// <param name="dec_values">The returned decision values.
        /// ATTENTION: The order of the dec_values are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which decides the order of dec_values.</param>
        /// <returns>The returned classification labels.</returns>
        public double[] PredictValues(LibSVMNode[][] xs, out double[][] dec_values)
        {
            int n = xs.GetLength(0); double[] labels = new double[n]; dec_values = new double[n][];
            for (int i = 0; i < n; i++)
            {
                labels[i] = PredictValues(xs[i], out dec_values[i]);
            }
            return labels;
        }

        /// <summary>
        /// Call svm_predict_values function. 
        /// SVM classification of a set of samples. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="xs">The sample feature vector, use NaN for missing features.</param>
        /// <param name="dec_values">The returned decision values.
        /// ATTENTION: The order of the dec_values are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which decides the order of dec_values.</param>
        /// <returns>The returned classification labels.</returns>
        public double[] PredictValues(List<List<double>> xs, out double[][] dec_values)
        {
            int n = xs.Count; double[] labels = new double[n]; dec_values = new double[n][];
            for (int i = 0; i < n; i++)
            {
                labels[i] = PredictValues(xs[i], out dec_values[i]);
            }
            return labels;
        }

        /// <summary>
        /// Call svm_predict_values function. 
        /// SVM classification of a set of samples. If want to predict the label only, call Predict().
        /// </summary>
        /// <param name="xs">The sample feature arrays, use NaN for missing features.</param>
        /// <param name="dec_values">The returned decision values.
        /// ATTENTION: The order of the dec_values are decided by the svm_model.
        /// They may not be in accending or decending orders.
        /// Labels property stores the labels of this svm classification, which decides the order of dec_values.</param>
        /// <returns>The returned classification labels.</returns>
        public double[] PredictValues(double[][] xs, out double[][] dec_values)
        {
            int n = xs.GetLength(0); double[] labels = new double[n]; dec_values = new double[n][];
            for (int i = 0; i < n; i++)
            {
                labels[i] = PredictValues(xs[i], out dec_values[i]);
            }
            return labels;
        }

        #endregion

        #region scale data
        /// <summary>
        /// Read LibSVM range date file.
        /// The file is generated using svm-scale.exe tool of LibSVM
        /// Note: y scaling is not supported.
        /// </summary>
        /// <param name="range_file_path">The file path generated using svm-scale.exe</param>
        /// <param name="targe_min">The targe min</param>
        /// <param name="targe_max">The targe max</param>
        /// <param name="features_min">The minimums of all features</param>
        /// <param name="features_max">The maximums of all features</param>
        public static void ReadRange(string range_file_path, out double targe_min, out double targe_max, out double[] features_min, out double[] features_max)
        {
            string line; string[] words; int id, id_max = -1; double min, max;
            List<int> ids = new List<int>(); List<double> mins = new List<double>(), maxs = new List<double>();
            using (StreamReader sr = new StreamReader(range_file_path))
            {
                line = sr.ReadLine();   //Read x
                line = sr.ReadLine();   //Target
                words = line.Split(); targe_min = double.Parse(words[0]); targe_max = double.Parse(words[1]);

                while ((line = sr.ReadLine()) != null) //Read min, max for each feature
                {
                    words = line.Split();
                    id = int.Parse(words[0]); min = double.Parse(words[1]); max = double.Parse(words[2]);

                    ids.Add(id); mins.Add(min); maxs.Add(max);
                    if (id > id_max) id_max = id;
                }
            }

            features_min = new double[id_max]; features_max = new double[id_max];
            for (int i = 0; i < ids.Count; i++)
            {
                features_min[ids[i] - 1] = mins[i];
                features_max[ids[i] - 1] = maxs[i];
            }
        }

        /// <summary>
        /// To scale the test data
        /// </summary>
        /// <param name="orig_data">Original test data in LibSVM format.</param>
        /// <param name="targe_min">The target min</param>
        /// <param name="targe_max">The target max</param>
        /// <param name="features_min">The minimums of all features</param>
        /// <param name="features_max">The maximums of all features</param>
        /// <returns>The scaled test data in LibSVM format.</returns>
        public static LibSVMNode[] ScaleData(LibSVMNode[] orig_data, double targe_min, double targe_max, double[] features_min, double[] features_max)
        {
            int feature_n = features_min.Length;
            double[] k = new double[feature_n];
            for (int i = 0; i < feature_n; i++)
                k[i] = (targe_max - targe_min) / (features_max[i] - features_min[i]);

            int n = orig_data.Length;
            LibSVMNode[] scaled_data = new LibSVMNode[n];
            for (int i = 0; i < n; i++)
            {
                int index = orig_data[i].index;
                scaled_data[i].index = index; if (index == -1) break;
                scaled_data[i].value = k[index - 1] * (orig_data[i].value - features_min[index - 1]) + targe_min;
            }
            return scaled_data;
        }

        /// <summary>
        /// To scale the test data
        /// </summary>
        /// <param name="orig_data">Original test data, use NaN for missing features.</param>
        /// <param name="targe_min">The target min</param>
        /// <param name="targe_max">The target max</param>
        /// <param name="features_min">The minimums of all features</param>
        /// <param name="features_max">The maximums of all features</param>
        /// <returns>The scaled test data in LibSVM format.</returns>
        public static LibSVMNode[] ScaleData(double[] orig_data, double targe_min, double targe_max, double[] features_min, double[] features_max)
        {
            int feature_n = features_min.Length;
            double[] k = new double[feature_n];
            for (int i = 0; i < feature_n; i++)
                k[i] = (targe_max - targe_min) / (features_max[i] - features_min[i]);

            List<LibSVMNode> scaled_data = new List<LibSVMNode>(); LibSVMNode node;
            for (int i = 0; i < feature_n; i++)
            {
                if (double.IsNaN(orig_data[i])) continue;

                node = new LibSVMNode();
                node.index = i + 1;
                node.value = k[i] * (orig_data[i] - features_min[i]) + targe_min;
                scaled_data.Add(node);
            }
            node = new LibSVMNode(); node.index = -1; scaled_data.Add(node);
            return scaled_data.ToArray();
        }

        /// <summary>
        /// To scale the test data
        /// </summary>
        /// <param name="orig_data">Original test data, use NaN for missing features.</param>
        /// <param name="targe_min">The target min</param>
        /// <param name="targe_max">The target max</param>
        /// <param name="features_min">The minimums of all features</param>
        /// <param name="features_max">The maximums of all features</param>
        /// <returns>The scaled test data in LibSVM format.</returns>
        public static LibSVMNode[] ScaleData(List<double> orig_data, double targe_min, double targe_max, double[] features_min, double[] features_max)
        {
            int feature_n = features_min.Length;
            double[] k = new double[feature_n];
            for (int i = 0; i < feature_n; i++)
                k[i] = (targe_max - targe_min) / (features_max[i] - features_min[i]);

            List<LibSVMNode> scaled_data = new List<LibSVMNode>(); LibSVMNode node;
            for (int i = 0; i < feature_n; i++)
            {
                if (double.IsNaN(orig_data[i])) continue;

                node = new LibSVMNode();
                node.index = i + 1;
                node.value = k[i] * (orig_data[i] - features_min[i]) + targe_min;
                scaled_data.Add(node);
            }
            node = new LibSVMNode(); node.index = -1; scaled_data.Add(node);
            return scaled_data.ToArray();
        }

        /// <summary>
        /// To scale the test data
        /// </summary>
        /// <param name="orig_data">Original test data, use NaN for missing features.</param>
        /// <param name="targe_min">The target min</param>
        /// <param name="targe_max">The target max</param>
        /// <param name="features_min">The minimums of all features</param>
        /// <param name="features_max">The maximums of all features</param>
        /// <returns>The scaled test data in LibSVM format.</returns>
        public static LibSVMNode[][] ScaleData(double[][] orig_data, double targe_min, double targe_max, double[] features_min, double[] features_max)
        {
            int feature_n = features_min.Length;
            double[] k = new double[feature_n];
            for (int i = 0; i < feature_n; i++)
                k[i] = (targe_max - targe_min) / (features_max[i] - features_min[i]);

            int sample_n = orig_data.GetLength(0);
            LibSVMNode[][] data = new LibSVMNode[sample_n][];
            for (int m = 0; m < sample_n; m++)
            {
                List<LibSVMNode> scaled_data = new List<LibSVMNode>(); LibSVMNode node;
                for (int i = 0; i < feature_n; i++)
                {
                    if (double.IsNaN(orig_data[m][i])) continue;

                    node = new LibSVMNode();
                    node.index = i + 1;
                    node.value = k[i] * (orig_data[m][i] - features_min[i]) + targe_min;
                    scaled_data.Add(node);
                }
                node = new LibSVMNode(); node.index = -1; scaled_data.Add(node);
                data[m] = scaled_data.ToArray();
            }
            return data;
        }

        /// <summary>
        /// To scale the test data
        /// </summary>
        /// <param name="orig_data">Original test data, use NaN for missing features.</param>
        /// <param name="targe_min">The target min</param>
        /// <param name="targe_max">The target max</param>
        /// <param name="features_min">The minimums of all features</param>
        /// <param name="features_max">The maximums of all features</param>
        /// <returns>The scaled test data in LibSVM format.</returns>
        public static LibSVMNode[][] ScaleData(List<List<double>> orig_data, double targe_min, double targe_max, double[] features_min, double[] features_max)
        {
            int feature_n = features_min.Length;
            double[] k = new double[feature_n];
            for (int i = 0; i < feature_n; i++)
                k[i] = (targe_max - targe_min) / (features_max[i] - features_min[i]);

            int sample_n = orig_data.Count;
            LibSVMNode[][] data = new LibSVMNode[sample_n][];
            for (int m = 0; m < sample_n; m++)
            {
                List<LibSVMNode> scaled_data = new List<LibSVMNode>(); LibSVMNode node;
                for (int i = 0; i < feature_n; i++)
                {
                    if (double.IsNaN(orig_data[m][i])) continue;

                    node = new LibSVMNode();
                    node.index = i + 1;
                    node.value = k[i] * (orig_data[m][i] - features_min[i]) + targe_min;
                    scaled_data.Add(node);
                }
                node = new LibSVMNode(); node.index = -1; scaled_data.Add(node);
                data[m] = scaled_data.ToArray();
            }
            return data;
        }

        #endregion

        #region PInvoke to unmanaged functions
        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double svm_predict(IntPtr model_p, IntPtr x_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int svm_get_svm_type(IntPtr model_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int svm_get_nr_class(IntPtr model_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void svm_get_labels(IntPtr model_p, IntPtr labels_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double svm_predict_probability(IntPtr model_p, IntPtr x_p, IntPtr probs_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double svm_predict_values(IntPtr model_p, IntPtr x_p, IntPtr dec_values_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr svm_load_model(string model_file);
        //private static extern IntPtr svm_load_model(IntPtr model_file);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int svm_free_and_destroy_model(IntPtr model_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int svm_destroy_model(IntPtr model_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int svm_get_nr_sv(IntPtr model_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int svm_get_nr_feature(IntPtr model_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int svm_get_sv(IntPtr model_p, int i, IntPtr sv_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double svm_get_rho(IntPtr model_p);

        [DllImport("libsvm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int svm_get_coef(IntPtr model_p, IntPtr coef_p);

        #endregion

        #region Static functions for converting to LibSVM sample format

        /// <summary>
        /// Convert sample features to LibSVM format
        /// </summary>
        /// <param name="sample_s">sample feature in string, i.e., one record in LibSVM file. "Target index:value index:value ..."</param>
        /// <param name="sample">the converted sample feature in LibSVM format.</param>
        /// <param name="label">the label</param>
        public static void ToLibSVMFormat(string sample_s, out LibSVMNode[] sample, out double label)
        {
            string[] words = sample_s.Trim().Split();
            label = double.Parse(words[0]);
            sample = new LibSVMNode[words.Length];
            for (int i = 1; i < words.Length; i++)
            {
                string[] ws = words[i].Split(new char[] { ':' });
                sample[i - 1].index = int.Parse(ws[0]);
                sample[i - 1].value = double.Parse(ws[1]);
            }
            sample[words.Length - 1].index = -1;
        }

        /// <summary>
        /// Convert sample features to LibSVM format
        /// </summary>
        /// <param name="features">The features, with missing features indicated with NaN</param>
        /// <returns>the converted sample feature in LibSVM format.</returns>
        private static LibSVMNode[] ToLibSVMFormat(List<double> features)
        {
            List<LibSVMNode> sample = new List<LibSVMNode>(); LibSVMNode node;

            for (int i = 0; i < features.Count; i++)
            {
                if (double.IsNaN(features[i])) continue;
                node.index = i + 1; node.value = features[i];
                sample.Add(node);
            }
            node.index = -1; node.value = 0.0; sample.Add(node);
            return sample.ToArray();
        }

        /// <summary>
        /// Convert sample features to LibSVM format
        /// </summary>
        /// <param name="features">The features, with missing features indicated with NaN</param>
        /// <returns>the converted sample feature in LibSVM format.</returns>
        private static LibSVMNode[] ToLibSVMFormat(double[] features)
        {
            List<LibSVMNode> sample = new List<LibSVMNode>(); LibSVMNode node;

            for (int i = 0; i < features.Length; i++)
            {
                if (double.IsNaN(features[i])) continue;
                node.index = i + 1; node.value = features[i];
                sample.Add(node);
            }
            node.index = -1; node.value = 0.0; sample.Add(node);
            return sample.ToArray();
        }

        /// <summary>
        /// Convert sample features to LibSVM format
        /// </summary>
        /// <param name="features">The features, with missing features indicated with NaN</param>
        /// <returns>the converted sample feature in LibSVM format.</returns>
        private static LibSVMNode[] ToLibSVMFormat(float[] features)
        {
            List<LibSVMNode> sample = new List<LibSVMNode>(); LibSVMNode node;

            for (int i = 0; i < features.Length; i++)
            {
                if (float.IsNaN(features[i])) continue;
                node.index = i + 1; node.value = features[i];
                sample.Add(node);
            }
            node.index = -1; node.value = 0.0; sample.Add(node);
            return sample.ToArray();
        }

        /// <summary>
        /// Save samples in LibSVM format. Mostly for preparing data to use LibSVM tools to train a SVM model
        /// </summary>
        /// <param name="filename">The file path.</param>
        /// <param name="targets">The targets: classification labels or regression targets.</param>
        /// <param name="features">The features, use NaN for missing features.</param>
        public static void SaveInLibSVMFormat(string filename, List<double> targets, List<float[]> features)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    sw.Write(targets[i].ToString());
                    for (int j = 0; j < features[i].Length; j++)
                    {
                        if (float.IsNaN(features[i][j])) continue;

                        int index = j + 1; double value = features[i][j];
                        sw.Write(" " + index.ToString() + ":" + value.ToString());
                    }
                    sw.WriteLine();
                }
            }
        }

        /// <summary>
        /// Save samples in LibSVM format. Mostly for preparing data to use LibSVM tools to train a SVM model
        /// </summary>
        /// <param name="filename">The file path.</param>
        /// <param name="targets">The targets: classification labels or regression targets.</param>
        /// <param name="features">The features, use NaN for missing features.</param>
        public static void SaveInLibSVMFormat(string filename, List<double> targets, List<List<double>> features)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    sw.Write(targets[i].ToString());
                    for (int j = 0; j < features[i].Count; j++)
                    {
                        if (double.IsNaN(features[i][j])) continue;

                        int index = j + 1; double value = features[i][j];
                        sw.Write(" " + index.ToString() + ":" + value.ToString());
                    }
                    sw.WriteLine();
                }
            }
        }

        /// <summary>
        /// Save samples in LibSVM format. Mostly for preparing data to use LibSVM tools to train a SVM model
        /// </summary>
        /// <param name="filename">The file path.</param>
        /// <param name="targets">The targets: classification labels or regression targets.</param>
        /// <param name="features">The features, use NaN for missing features.</param>
        public static void SaveInLibSVMFormat(string filename, double[] targets, double[][] features)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    sw.Write(targets[i].ToString());
                    for (int j = 0; j < features[i].Length; j++)
                    {
                        if (double.IsNaN(features[i][j])) continue;

                        int index = j + 1; double value = features[i][j];
                        sw.Write(" " + index.ToString() + ":" + value.ToString());
                    }
                    sw.WriteLine();
                }
            }
        }

        /// <summary>
        /// Save samples in LibSVM format. Mostly for preparing data to use LibSVM tools to train a SVM model
        /// </summary>
        /// <param name="filename">The file path.</param>
        /// <param name="targets">The targets: classification labels or regression targets.</param>
        /// <param name="features">The features, use NaN for missing features.</param>
        public static void SaveInLibSVMFormat(string filename, double[] targets, double[,] features)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    sw.Write(targets[i].ToString());
                    for (int j = 0; j < features.GetLength(1); j++)
                    {
                        if (double.IsNaN(features[i, j])) continue;

                        int index = j + 1; double value = features[i, j];
                        sw.Write(" " + index.ToString() + ":" + value.ToString());
                    }
                    sw.WriteLine();
                }
            }
        }

        /// <summary>
        /// Save samples in LibSVM format. Mostly for preparing data to use LibSVM tools to train a SVM model
        /// </summary>
        /// <param name="filename">The file path.</param>
        /// <param name="targets">The targets: classification labels or regression targets.</param>
        /// <param name="features">The features, use NaN for missing features.</param>
        public static void SaveInLibSVMFormat(string filename, float[] targets, float[][] features)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    sw.Write(targets[i].ToString());
                    for (int j = 0; j < features[i].Length; j++)
                    {
                        if (float.IsNaN(features[i][j])) continue;

                        int index = j + 1; float value = features[i][j];
                        sw.Write(" " + index.ToString() + ":" + value.ToString());
                    }
                    sw.WriteLine();
                }
            }
        }

        #endregion
    }
}
