﻿using Aggregates.Contracts;
using Aggregates.Messages;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aggregates.UnitTests.Common
{
    [TestFixture]
    public class UnitOfWork
    {
        public class FakeState : State<FakeState> { }
        public class FakeEntity : Entity<FakeEntity, FakeState> { }
        public class FakePoco { }

        private Moq.Mock<IRepositoryFactory> _factory;
        private Moq.Mock<IEventFactory> _eventFactory;
        private Moq.Mock<IProcessor> _processor;
        private Moq.Mock<IRepository<FakeEntity>> _repository;
        private Moq.Mock<IPocoRepository<FakePoco>> _pocoRepository;
        private Moq.Mock<IDomainUnitOfWork> _uow;

        private Aggregates.Internal.UnitOfWork _unitofwork;

        [SetUp]
        public void Setup()
        {
            _factory = new Moq.Mock<IRepositoryFactory>();
            _eventFactory = new Moq.Mock<IEventFactory>();
            _processor = new Moq.Mock<IProcessor>();
            _repository = new Moq.Mock<IRepository<FakeEntity>>();
            _pocoRepository = new Moq.Mock<IPocoRepository<FakePoco>>();
            _uow = new Moq.Mock<IDomainUnitOfWork>();

            _factory.Setup(x => x.ForEntity<FakeEntity>(Moq.It.IsAny<IDomainUnitOfWork>())).Returns(_repository.Object);
            _factory.Setup(x => x.ForPoco<FakePoco>(Moq.It.IsAny<IDomainUnitOfWork>())).Returns(_pocoRepository.Object);

            _unitofwork = new Internal.UnitOfWork(_factory.Object, _eventFactory.Object, _processor.Object);
        }

        [Test]
        public void dispose_repository_disposed()
        {
            var repo = _unitofwork.For<FakeEntity>();
            var poco = _unitofwork.Poco<FakePoco>();

            _repository.Setup(x => x.Dispose());
            _pocoRepository.Setup(x => x.Dispose());

            _unitofwork.Dispose();

            _repository.Verify(x => x.Dispose(), Moq.Times.Once);
            _pocoRepository.Verify(x => x.Dispose(), Moq.Times.Once);
        }

        [Test]
        public async Task commit_repository()
        {
            var repo = _unitofwork.For<FakeEntity>();
            var poco = _unitofwork.Poco<FakePoco>();

            _repository.Setup(x => x.Commit(Moq.It.IsAny<Guid>(), Moq.It.IsAny<IDictionary<string, string>>())).Returns(Task.CompletedTask);
            _pocoRepository.Setup(x => x.Commit(Moq.It.IsAny<Guid>(), Moq.It.IsAny<IDictionary<string, string>>())).Returns(Task.CompletedTask);

            await (_unitofwork as IDomainUnitOfWork).End();

            _repository.Verify(x => x.Commit(Moq.It.IsAny<Guid>(), Moq.It.IsAny<IDictionary<string, string>>()), Moq.Times.Once);
            _pocoRepository.Verify(x => x.Commit(Moq.It.IsAny<Guid>(), Moq.It.IsAny<IDictionary<string, string>>()), Moq.Times.Once);
        }

        [Test]
        public async Task multiple_changes_prepare()
        {
            var repo = _unitofwork.For<FakeEntity>();
            var poco = _unitofwork.Poco<FakePoco>();

            _repository.Setup(x => x.ChangedStreams).Returns(2);
            _pocoRepository.Setup(x => x.ChangedStreams).Returns(2);

            _repository.Setup(x => x.Prepare(Moq.It.IsAny<Guid>())).Returns(Task.CompletedTask);
            _pocoRepository.Setup(x => x.Prepare(Moq.It.IsAny<Guid>())).Returns(Task.CompletedTask);

            await (_unitofwork as IDomainUnitOfWork).End();

            _repository.Verify(x => x.Prepare(Moq.It.IsAny<Guid>()), Moq.Times.Once);
            _pocoRepository.Verify(x => x.Prepare(Moq.It.IsAny<Guid>()), Moq.Times.Once);

        }

        [Test]
        public async Task commit_exception_no_commit()
        {
            var repo = _unitofwork.For<FakeEntity>();
            var poco = _unitofwork.Poco<FakePoco>();

            _repository.Setup(x => x.Commit(Moq.It.IsAny<Guid>(), Moq.It.IsAny<IDictionary<string, string>>())).Returns(Task.CompletedTask);
            _pocoRepository.Setup(x => x.Commit(Moq.It.IsAny<Guid>(), Moq.It.IsAny<IDictionary<string, string>>())).Returns(Task.CompletedTask);

            await(_unitofwork as IDomainUnitOfWork).End(new Exception());

            _repository.Verify(x => x.Commit(Moq.It.IsAny<Guid>(), Moq.It.IsAny<IDictionary<string, string>>()), Moq.Times.Never);
            _pocoRepository.Verify(x => x.Commit(Moq.It.IsAny<Guid>(), Moq.It.IsAny<IDictionary<string, string>>()), Moq.Times.Never);
        }
    }
}
