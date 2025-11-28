# FacialRecognitionApp

**FacialRecognitionApp** est une application Windows Forms en **C#** utilisant **OpenCvSharp** pour la reconnaissance faciale.  
Elle combine **LBPH** pour la reconnaissance locale et **DNN** pour la détection de visage en temps réel.

---

## 🚀 Fonctionnalités clés

- **Form1** : Connexion Admin / Client  
- **AdminForm** : Ajouter, supprimer et entraîner des visages (LBPH)  
- **ClientForm** : Reconnaissance faciale temps réel via webcam  
- Détection de visages avec **Haar Cascade** ou **DNN**  
- Sauvegarde du modèle LBPH et des labels dans `TrainingData/`  
- Gestion des exceptions et logs robustes

---

## 🔗 Ressources

- Haar Cascade: [haarcascade_frontalface_alt.xml](https://github.com/opencv/opencv/blob/master/data/haarcascades/haarcascade_frontalface_alt.xml)  
- DNN Model: [deploy.prototxt](https://github.com/opencv/opencv/blob/master/samples/dnn/face_detector/deploy.prototxt), [res10_300x300_ssd_iter_140000.caffemodel](https://github.com/opencv/opencv_3rdparty/blob/dnn_samples_face_detector_20170830/res10_300x300_ssd_iter_140000.caffemodel)

---

## ⚡ Usage rapide

1. Cloner le dépôt  
2. Ouvrir la solution dans **Visual Studio 2022**  
3. Restaurer les packages NuGet et **Build**  
4. Lancer l’application  
5. Ajouter des visages (Admin) et tester la reconnaissance (Client)  

---

## 🛠️ Tech

C# (.NET 7 / WinForms), OpenCvSharp, LBPH, DNN

