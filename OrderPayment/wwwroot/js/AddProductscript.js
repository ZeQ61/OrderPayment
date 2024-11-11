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

            // Ürünü local storage'a ekle
            const products = JSON.parse(localStorage.getItem('products') || '[]');
            products.push({ name, stock, unit, price });
            localStorage.setItem('products', JSON.stringify(products));

            // Formu temizle
            this.reset();

            alert('Ürün başarıyla eklendi!');
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
            `;
        });
    }
});