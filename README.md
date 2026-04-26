# Attendance Management System (AMS)

AMS is a sophisticated, web-based platform designed to automate student attendance through advanced facial recognition and real-time monitoring. Built on .NET 8 and Blazor Server, the system provides a robust architecture for educational institutions to manage classes, student profiles, and attendance reporting with high precision.

## 🚀 Key Features

- **Automated Attendance**: Real-time facial recognition via webcam for instant attendance logging.
- **Comprehensive Reporting**: Detailed analytics including class-wise average attendance and student-specific trends.
- **Schedule Management**: Automated generation of yearly class schedules with flexible modification options.
- **Dashboard Analytics**: Executive overview for admins and personalized views for trainers/teachers.
- **Profile Management**: Secure storage and retrieval of student information and bio-data.

---

## 🧠 Face Recognition Engine

The core of the AMS platform is its high-performance facial recognition engine, implemented using `face-api.js`, which leverages TensorFlow.js in the browser environment.

### Technical Architecture
The engine operates on a decentralized model where heavy feature extraction and matching are performed on the client-side to ensure low latency and reduced server load.

#### 1. Neural Network Models
The system utilizes three quantized pre-trained models for distinct stages of the recognition pipeline:
- **SSD MobileNet V1**: A Single Shot Multibox Detector with a MobileNet backbone for rapid and accurate face detection within the video stream.
- **68-Point Face Landmark Net**: Extracted landmarks (eyes, nose, mouth, jawline) are used to align the face, ensuring the recognition is invariant to minor head tilts or rotations.
- **Face Recognition Net**: A ResNet-34 based architecture that compresses facial features into a **128-dimensional float32 vector (face descriptor)**.

#### 2. Vector Matching (FaceMatcher)
During the initialization phase, the system generates a **Labeled Face Descriptor** for every student assigned to the current session using previously verified profile images. These descriptors are loaded into a `FaceMatcher` instance.
- **Euclidean Distance**: Recognition is performed by calculating the Euclidean distance between the real-time probe descriptor and the library of labeled descriptors.
- **Confidence Threshold**: A standard threshold of **0.6** is applied. A distance lower than this threshold constitutes a successful identification.

#### 3. Real-time Pipeline
```mermaid
graph LR
    A[Webcam Feed] --> B[Face Detection]
    B --> C[Landmark Extraction]
    C --> D[Descriptor Generation]
    D --> E[Vector Comparison]
    E -- Match Found --> F[Blazor SignalR Invoke]
    F --> G[DB Record Update]
```

#### 4. Blazor / JavaScript Bridge
The integration is achieved through the **IJSRuntime** abstraction.
- **Initialization**: Blazor passes student IDs and image paths to the JS context.
- **Callback**: Upon a successful match, the JS engine invokes a `[JSInvokable]` method in C# using a `DotNetObjectReference`, ensuring the UI and database are updated in real-time without manual polling.

---

## 🛠 Tech Stack

- **Backend**: .NET 8 / C#
- **Frontend**: Blazor Server (Interactive Server Render Mode)
- **Database**: Entity Framework Core with SQL Server/PostgreSQL
- **Facial Recognition**: face-api.js / TensorFlow.js
- **UI Framework**: Bootstrap 5 with custom CSS3 modern aesthetics

---

## 🛠 Setup and Installation

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/your-repo/ams.git
   ```

2. **Database Configuration**:
   Update the connection string in `appsettings.json` to point to your database instance.

3. **Apply Migrations**:
   ```bash
   dotnet ef database update
   ```

4. **Run the Application**:
   ```bash
   dotnet run
   ```

---

## 📝 License
This project is licensed under the MIT License - see the LICENSE file for details.
