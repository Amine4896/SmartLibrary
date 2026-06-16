pipeline {
    agent any

    stages {
        stage('Checkout') {
            steps {
                echo 'Récupération du code depuis Git...'
                checkout scm
            }
        }
        stage('Restore') {
            steps {
                echo 'Restauration des packages NuGet...'
                bat 'dotnet restore'
            }
        }
        stage('Build') {
            steps {
                echo 'Compilation du projet en configuration Release...'
                bat 'dotnet build --configuration Release --no-restore'
            }
        }
        stage('Test') {
            steps {
                echo 'Exécution des tests unitaires...'
                bat 'dotnet test --no-build'
            }
        }
        stage('Publish') {
            steps {
                echo 'Publication de l\'application...'
                bat 'dotnet publish --configuration Release --output ./publish'
            }
        }
    }

    post {
        success {
            echo '✅ Build et tests réussis avec succès !'
        }
        failure {
            echo '❌ Échec du build ou des tests.'
        }
    }
}