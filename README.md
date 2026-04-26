# Attendance Management System (AMS)

AMS is a sophisticated, web-based platform designed to automate student attendance through advanced facial recognition and real-time monitoring. Built on .NET 8 and Blazor Server, the system provides a robust architecture for educational institutions to manage classes, student profiles, and attendance reporting with high precision.

## 🚀 Key Features

### 1. Identity & Access Management
* **Role-Based Security:** Secure login system using .NET Identity for **Admins** and **Teachers**.
* **User Management:** Admin capability to manage system users and assign specific permissions.
  
  <img width="1914" height="967" alt="login" src="https://github.com/user-attachments/assets/73fabb38-c006-4fd3-a5c6-d0a858410ad0" />
  
  <img width="3140" height="3326" alt="dashboard" src="https://github.com/user-attachments/assets/7dcf4880-3bff-430f-9fa1-d0973ed4eaa0" />
  
  <img width="1912" height="960" alt="SystemSettings" src="https://github.com/user-attachments/assets/7a571e26-d359-40e7-80d4-f563c5705b98" />

### 2. Academic Orchestration
* **Student Profiles:** Centralized database for student bio-data and 128-float facial descriptors.
* **Schedule Management:** Module to define yearly class schedules, time slots, and room assignments.
* **Class Assignment:** Link students to specific courses to create accurate expected-attendee lists.
 <img width="1915" height="952" alt="StudentProfile" src="https://github.com/user-attachments/assets/56800f6d-7854-40e9-878f-44f0be53e111" />


### 3. Real-Time Operations & AI
* **Automated Attendance:** Instant recognition using `face-api.js` and TensorFlow.js for contactless logging.
* **Live Dashboard:** Real-time data visualization showing current attendance stats and system health.
* **Instant Email Notifications:** Integrated **MailKit** service that sends a "Digital Receipt" to students/guardians immediately after recognition.
<img width="1917" height="968" alt="Attendaces" src="https://github.com/user-attachments/assets/8fd00645-3459-467e-bcca-ef59cf0ff507" />


### 4. Comprehensive Reporting Engine
* **Analytical Summaries:** View class-wise and student-wise average attendance percentages.
* **Periodic History:** Generate weekly and monthly attendance reports to track long-term trends.
* **Excel Export:** Specialized "Course Date-wise" detailed report exportable to **.xlsx** for external auditing.
<img width="1907" height="948" alt="AttendanceReport" src="https://github.com/user-attachments/assets/34c00c4a-9377-4c46-a419-76fdedd7e840" />

<img width="1912" height="478" alt="AttendanceReportSummary" src="https://github.com/user-attachments/assets/ab6aa0b8-8183-451b-976f-397e6349834e" />

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
   git clone https://github.com/labibmahir/AMS_Public.git
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
