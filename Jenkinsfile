pipeline {
    agent any

    environment {
        DOTNET_ROOT = "/root/.dotnet"
        PATH = "/root/.dotnet:/root/.dotnet/tools:${env.PATH}"
    }

    stages {
        stage('Checkout') {
            steps {
                echo 'Checkout...'
                checkout scm
            }
        }
        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }
        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release --no-restore'
            }
        }
        stage('Test') {
            steps {
                sh 'dotnet test --no-build'
            }
        }
        stage('Publish') {
            steps {
                sh 'dotnet publish --configuration Release --output ./publish'
            }
        }
    }

    post {
        success { echo '✅ Build réussi !' }
        failure { echo '❌ Build échoué.' }
    }
}