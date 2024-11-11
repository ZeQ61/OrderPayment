document.addEventListener('DOMContentLoaded', function () {
    const productForm = document.getElementById('productForm');
    const productTable = document.getElementById('productTable');

    if (productForm) {
        productForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const name = document.getElementById('name').value;
            const stock = document.getElementById('stock').value;
            const unit = document.getElementById('unit').value;
            const price = document.getElementById('price').value;
            const category = document.getElementById('category').value;
            const image = document.getElementById('image').files[0];

            // Resmi yükleme ve URL'yi alma
            let imageUrl = '';
            if (image) {
                const reader = new FileReader();
                reader.onloadend = function () {
                    imageUrl = reader.result;  // Base64 formatında resim alındı.
                    // Ürünü localStorage'a ekle
                    const products = JSON.parse(localStorage.getItem('products') || '[]');
                    products.push({ name, stock, unit, price, category, image: imageUrl });
                    localStorage.setItem('products', JSON.stringify(products));

                    // Formu temizle
                    productForm.reset();

                    alert('Ürün başarıyla eklendi!');
                    location.reload(); // Sayfayı yeniden yükle
                };
                reader.readAsDataURL(image); // Resmi okuyup base64 formatında al
            }
        });
    }

    if (productTable) {
        // Ürünleri listele
        const products = JSON.parse(localStorage.getItem('products') || '[]');
        const tbody = productTable.getElementsByTagName('tbody')[0];

        products.forEach(product => {
            const newRow = tbody.insertRow();
            newRow.innerHTML = `
                <td>${product.name}</td>
                <td>${product.stock}</td>
                <td>${product.unit}</td>
                <td>${parseFloat(product.price).toFixed(2)}</td>
                <td>${product.category}</td>
                <td>
                    <img src="${product.image}" alt="Ürün Resmi" style="width: 50px; height: 50px;">
                </td>
            `;
        });
    }
});
