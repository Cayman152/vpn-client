package com.ghostvpn.android.vpn

import android.os.Build
import java.io.BufferedInputStream
import java.io.File
import java.io.FileOutputStream
import java.net.HttpURLConnection
import java.net.URL
import java.util.zip.ZipEntry
import java.util.zip.ZipInputStream

class XrayBinaryManager(private val workDir: File) {

    private val binDir = File(workDir, "bin")
    private val archiveDir = File(workDir, "archives")

    fun ensureBinary(): File {
        val binary = File(binDir, "xray")
        if (binary.exists() && binary.canExecute()) {
            return binary
        }

        binDir.mkdirs()
        archiveDir.mkdirs()

        val candidates = buildAssetCandidates()
        var lastError: Throwable? = null

        for (assetName in candidates) {
            try {
                val archiveFile = File(archiveDir, assetName)
                downloadArchive(assetName, archiveFile)
                extractBinary(archiveFile, binary)
                binary.setExecutable(true)
                return binary
            } catch (e: Throwable) {
                lastError = e
            }
        }

        throw IllegalStateException(
            "Не удалось скачать xray для ABI ${Build.SUPPORTED_ABIS.joinToString()}",
            lastError
        )
    }

    private fun buildAssetCandidates(): List<String> {
        val abi = Build.SUPPORTED_ABIS.firstOrNull().orEmpty().lowercase()
        return when {
            abi.contains("arm64") -> listOf(
                "Xray-android-arm64-v8a.zip",
                "Xray-android-arm64.zip"
            )

            abi.contains("armeabi") || abi.contains("arm") -> listOf(
                "Xray-android-arm32-v7a.zip",
                "Xray-android-arm32.zip"
            )

            abi.contains("x86_64") || abi.contains("amd64") -> listOf(
                "Xray-android-amd64.zip",
                "Xray-android-x64.zip"
            )

            abi.contains("x86") -> listOf(
                "Xray-android-386.zip",
                "Xray-android-x86.zip"
            )

            else -> listOf("Xray-android-arm64-v8a.zip")
        }
    }

    private fun downloadArchive(assetName: String, destination: File) {
        val url = "https://github.com/XTLS/Xray-core/releases/latest/download/$assetName"
        val connection = URL(url).openConnection() as HttpURLConnection
        connection.instanceFollowRedirects = true
        connection.connectTimeout = 30_000
        connection.readTimeout = 60_000
        connection.setRequestProperty("User-Agent", "GhostVPN-Android")

        try {
            val code = connection.responseCode
            if (code !in 200..299) {
                throw IllegalStateException("Сервер вернул код $code для $assetName")
            }

            connection.inputStream.use { input ->
                FileOutputStream(destination).use { output ->
                    input.copyTo(output)
                }
            }
        } finally {
            connection.disconnect()
        }
    }

    private fun extractBinary(archiveFile: File, targetBinary: File) {
        ZipInputStream(BufferedInputStream(archiveFile.inputStream())).use { zip ->
            var entry: ZipEntry? = zip.nextEntry
            while (entry != null) {
                if (!entry.isDirectory && isXrayEntry(entry.name)) {
                    FileOutputStream(targetBinary).use { output ->
                        zip.copyTo(output)
                    }
                    return
                }
                zip.closeEntry()
                entry = zip.nextEntry
            }
        }

        throw IllegalStateException("В архиве ${archiveFile.name} не найден исполняемый файл xray")
    }

    private fun isXrayEntry(name: String): Boolean {
        val normalized = name.substringAfterLast('/')
        return normalized == "xray"
    }
}
