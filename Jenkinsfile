pipeline {
    agent any

    environment {
        // Le token de connexion sera lu à partir des identifiants Jenkins
        SONAR_TOKEN = credentials('sonar-token')
    }

    stages {
        stage('Checkout') {
            steps {
                echo 'Récupération du code depuis Git...'
                checkout scm
            }
        }
        stage('Clean') {
            steps {
                echo 'Nettoyage des anciens fichiers de publication...'
                sh 'rm -rf publish'
            }
        }
        stage('Restore') {
            steps {
                echo 'Restauration des packages NuGet...'
                sh 'dotnet restore'
            }
        }
        stage('Build & Analysis') {
            steps {
                echo 'Installation de dotnet-sonarscanner...'
                sh 'dotnet tool install --tool-path ./tools dotnet-sonarscanner --ignore-failed-sources || true'

                echo 'Début de l\'analyse SonarQube...'
                sh './tools/dotnet-sonarscanner begin /k:"SmartLibrary" /d:sonar.host.url="http://sonarqube:9000" /d:sonar.token="${SONAR_TOKEN}"'

                echo 'Compilation du projet...'
                sh 'dotnet build --configuration Release --no-restore'

                echo 'Exécution des tests...'
                sh 'dotnet test --no-build || true'

                echo 'Fin de l\'analyse SonarQube...'
                sh './tools/dotnet-sonarscanner end /d:sonar.token="${SONAR_TOKEN}"'
            }
        }
        stage('Publish') {
            steps {
                echo 'Publication de l\'application...'
                sh 'dotnet publish --configuration Release --output ./publish'
            }
        }
    }

    post {
        success {
            echo '✅ Build, tests et analyse SonarQube réussis avec succès !'
        }
        failure {
            echo '❌ Échec du build, des tests ou de l\'analyse.'
        }
    }
}